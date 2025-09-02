// File: ChabunGit/Services/PromptService.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// ▼▼▼ [수정] 아래 using 문들은 더 이상 필요 없으므로 삭제합니다 ▼▼▼
// using System.Net.Http;
using System.Text;
// using System.Text.Json;
// ▲▲▲ [수정] 여기까지 ▲▲▲
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly GitCommandExecutor _executor;
        // ▼▼▼ [수정] HttpClient 필드를 삭제합니다 ▼▼▼
        // private static readonly HttpClient _httpClient = new HttpClient();

        public PromptService(GitCommandExecutor executor)
        {
            _executor = executor;
        }

        // CreateInitialCommitPromptAsync, CreateGitignorePromptAsync 메서드는 그대로 유지합니다.
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

        // ▼▼▼ [수정] 아래 로직 전체를 새로운 단순한 코드로 대체합니다 ▼▼▼

        // 1. GetDiffAsync: 번역 없이 순수하게 diff 결과만 가져옵니다.
        public async Task<string> GetDiffAsync(string repoPath)
        {
            // --text : 바이너리로 보이더라도 텍스트 diff 시도
            // -U0 등 옵션 추가로 문맥 줄 제어 가능
            var diffResult = await _executor.ExecuteAsync(repoPath, "diff --text HEAD");

            if (diffResult.ExitCode != 0 || string.IsNullOrWhiteSpace(diffResult.Output))
            {
                // 혹시 output 이 여전히 비어있을 때 -a(모든 파일 텍스트 취급) 재시도
                var retry = await _executor.ExecuteAsync(repoPath, "diff -a HEAD");
                if (retry.ExitCode != 0 || string.IsNullOrWhiteSpace(retry.Output))
                    return "커밋할 변경 사항이 없습니다.";
                return retry.Output;
            }
            return diffResult.Output;
        }

        // 2. CreateCommitPrompt: diff 내용을 받아 AI 프롬프트를 생성합니다.
        public string CreateCommitPrompt(string diffContent)
        {
            return
$@"아래는 'git diff' 결과입니다. 이 변경 사항에 대한 커밋 메시지를 다음 규칙에 따라 영어로 작성해 주세요.

1.  **제목 (Subject):**
    * 변경 사항을 요약하는 한 줄의 제목을 작성합니다.
    * 제목 전체는 **50자를 넘지 않도록** 간결하게 작성합니다.
    * Conventional Commits 형식(`refactor:`, `feat:`, `fix:` 등)을 따라 영어로 작성합니다.
    * 명령형으로 작성합니다 (예: 'Fix' not 'Fixed', 'Add' not 'Added').
    * 제목 끝에 마침표를 찍지 마세요.

2.  **본문 (Body):**
    * 제목 아래에 한 줄을 비웁니다.
    * 무엇을, 왜 변경했는지 **자세히 설명**합니다. 한 줄이 아닌 **여러 줄로 작성**해도 좋습니다.
    * 어떻게 변경했는지는 코드 diff에 있으므로, 그보다는 **변경의 이유와 맥락**에 집중해주세요.
    * 그 다음, 한 줄을 비우고, 위 영문 항목들을 **한국어**로 동일하게 `-`를 사용하여 나열합니다.

3.  **출력 형식:**
    * 생성된 제목과 본문을 합쳐서, 아래 예시처럼 바로 사용할 수 있는 'git commit -m ""...""' 명령어 형식으로 만들어주세요.

[커밋 메시지 예시]
git commit -m ""refactor: Optimize hidden task counting logic

- Replace flat collection with hierarchical traversal
- Add early optimization when showHidden is enabled
- Implement recursive counting respecting parent hiding

- 평면화 방식을 계층적 순회로 교체
- showHidden 활성화시 조기 최적화 추가
- 부모 숨김 상태를 고려한 재귀 카운팅 구현""

이제 아래 diff 내용을 분석하여 커밋 메시지를 생성해 주세요.

diff 내용:
------------------------
{diffContent}
";
        }

        // 3. 기존의 번역 관련 #region은 모두 삭제합니다.
        // #region Private Translation Helpers ... #endregion 전체 삭제

        // ▲▲▲ [수정] 여기까지 ▲▲▲
    }
}
