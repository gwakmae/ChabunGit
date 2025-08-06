// File: ChabunGit/Services/PromptService.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly GitCommandExecutor _executor;
        private static readonly HttpClient _httpClient = new HttpClient();

        public PromptService(GitCommandExecutor executor)
        {
            _executor = executor;
        }

        public async Task<string> CreateInitialCommitPromptAsync(string repoPath)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("아래는 내 프로젝트의 전체 소스 코드입니다.");
            promptBuilder.AppendLine("이 프로젝트가 어떤 기능을 하는지 분석하고, 프로젝트의 첫 커밋(Initial Commit)에 어울리는 커밋 메시지를 생성해주세요.");
            promptBuilder.AppendLine("- 먼저 프로젝트에 대한 간단한 설명을 한글로 작성합니다.");
            promptBuilder.AppendLine("- 그리고 아래 형식에 맞춰 바로 사용할 수 있는 커밋 명령어를 영어로 생성합니다.");
            promptBuilder.AppendLine("(커밋 명령어 예시: git commit -m \"feat: Initial commit for ChabunGit project\")");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("--- 전체 코드 내용 ---");

            try
            {
                var ignoreDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", ".git", "node_modules" };
                var allFiles = Directory.GetFiles(repoPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    bool isIgnored = ignoreDirs.Any(dir => file.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar));
                    if (isIgnored) continue;

                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (new[] { ".exe", ".dll", ".pdb", ".png", ".jpg", ".ico", ".user", ".suo" }.Contains(ext)) continue;

                    string relativePath = Path.GetRelativePath(repoPath, file);
                    promptBuilder.AppendLine($"\n--- File: {relativePath} ---");
                    string content = await File.ReadAllTextAsync(file);
                    promptBuilder.AppendLine(content);
                }
            }
            catch (Exception ex)
            {
                promptBuilder.AppendLine($"\n파일을 읽는 중 오류 발생: {ex.Message}");
            }

            return promptBuilder.ToString();
        }

        public Task<string> CreateGitignorePromptAsync(string repoPath)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("아래는 내 프로젝트의 파일 및 폴더 구조에 대한 정보입니다.");
            promptBuilder.AppendLine("이 정보를 바탕으로, C# WPF 프로젝트에 최적화된 .gitignore 파일을 생성해주세요.");
            promptBuilder.AppendLine("특히 bin/, obj/ 폴더와 .user, .suo, .db, .log 확장자를 가진 파일들은 반드시 제외되어야 합니다.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("--- 프로젝트 정보 ---");

            try
            {
                var directories = Directory.GetDirectories(repoPath);
                var mainFolders = directories.Select(Path.GetFileName)
                                             .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith("."));
                promptBuilder.AppendLine($"주요 폴더: {string.Join(", ", mainFolders)}");

                var files = Directory.GetFiles(repoPath, "*.*", SearchOption.AllDirectories);
                var extensions = files
                    .Where(f => !f.Contains(Path.Combine(repoPath, ".git")))
                    .Select(Path.GetExtension)
                    .Where(ext => !string.IsNullOrEmpty(ext))
                    .GroupBy(ext => ext!.ToLower())
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key);

                promptBuilder.AppendLine($"주요 확장자: {string.Join(", ", extensions)}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"프로젝트 파일 분석 중 오류가 발생했습니다: {ex.Message}\n\nC# WPF 프로젝트를 위한 표준 .gitignore 파일을 생성해 주세요.");
            }

            return Task.FromResult(promptBuilder.ToString());
        }

        // 1단계: 번역된 diff만 반환하는 메서드
        public async Task<string> GetTranslatedDiffAsync(string repoPath)
        {
            var diffResult = await _executor.ExecuteAsync(repoPath, "diff HEAD");

            if (diffResult.ExitCode != 0 || string.IsNullOrWhiteSpace(diffResult.Output))
            {
                return "커밋할 변경 사항이 없습니다.";
            }

            return await TranslateGitDiffWithHTTP(diffResult.Output);
        }

        // 2단계: diff들을 받아 최종 AI 프롬프트를 만드는 메서드 (수정됨)
        public string CreateFinalPromptFromDiffs(string originalDiff, string translatedDiff)
        {
            // ★★★ GitDiffPromptGenerator의 프롬프트 형식과 100% 동일하게 수정 ★★★
            return
$@"아래는 'git diff' 결과입니다. 이 변경 사항에 대한 커밋 메시지를 다음 규칙에 따라 영어로 작성해 주세요.

1.  **제목 (Subject):**
    * 변경 사항을 요약하는 한 줄의 제목을 작성합니다.
    * **반드시 50자 이내**로 작성해야 합니다.
    * 명령형으로 작성합니다 (예: 'Fix' not 'Fixed', 'Add' not 'Added').
    * 제목 끝에 마침표를 찍지 마세요.

2.  **본문 (Body):**
    * 제목 아래에 한 줄을 비웁니다.
    * 무엇을, 왜 변경했는지 **자세히 설명**합니다. 한 줄이 아닌 **여러 줄로 작성**해도 좋습니다.
    * 어떻게 변경했는지는 코드 diff에 있으므로, 그보다는 **변경의 이유와 맥락**에 집중해주세요.

3.  **출력 형식:**
    * 생성된 제목과 본문을 합쳐서, 아래 예시처럼 바로 사용할 수 있는 'git commit -m ""...""' 명령어 형식으로 만들어주세요.

[커밋 메시지 예시]
git commit -m ""feat: Implement user authentication endpoint

- Add user registration and login logic.
- Use JWT for token-based authentication.
- Include basic validation for user input.""

이제 아래 diff 내용을 분석하여 커밋 메시지를 생성해 주세요.

diff 내용:
------------------------
{translatedDiff}
";
        }


        #region Private Translation Helpers
        private async Task<string> TranslateTextWithHTTP(string text)
        {
            try
            {
                var requestData = new
                {
                    q = text,
                    source = "en",
                    target = "ko",
                    format = "text"
                };

                string jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://libretranslate.de/translate", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseText);

                    if (responseObj.TryGetProperty("translatedText", out JsonElement translatedElement))
                    {
                        return translatedElement.GetString() ?? text;
                    }
                }
                return text;
            }
            catch
            {
                return text;
            }
        }

        private bool ContainsEnglishText(string text)
        {
            return text.Length > 5 &&
                       text.Any(c => char.IsLetter(c)) &&
                       text.Any(c => c >= 'A' && c <= 'z') &&
                       !text.StartsWith("diff --git") &&
                       !text.StartsWith("index ") &&
                       !text.StartsWith("@@");
        }

        private async Task<string> TranslateGitDiffWithHTTP(string englishDiff)
        {
            var result = new StringBuilder();
            result.AppendLine("=== Git Diff 결과 (번역됨) ===");
            result.AppendLine();

            var lines = englishDiff.Split('\n');

            foreach (var line in lines.Take(50))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string trimmedLine = line.Trim();
                string translatedLine = trimmedLine;

                if (trimmedLine.StartsWith("diff --git"))
                {
                    translatedLine = $"파일 비교: {trimmedLine.Substring(11)}";
                }
                else if (trimmedLine.StartsWith("--- "))
                {
                    translatedLine = $"이전 파일: {trimmedLine.Substring(4)}";
                }
                else if (trimmedLine.StartsWith("+++ "))
                {
                    translatedLine = $"새 파일: {trimmedLine.Substring(4)}";
                }
                else if (trimmedLine.StartsWith("@@"))
                {
                    translatedLine = $"변경 위치: {trimmedLine}";
                }
                else if (ContainsEnglishText(trimmedLine))
                {
                    translatedLine = await TranslateTextWithHTTP(trimmedLine);
                }

                result.AppendLine(translatedLine);
            }

            return result.ToString();
        }
        #endregion
    }
}