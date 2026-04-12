// File: ChabunGit/Services/PromptService.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly GitCommandExecutor _executor;
        private readonly HttpClient _httpClient;

        private const string OLLAMA_MODEL_NAME = "qwen2.5-coder:14b";

        // AI에게 전달할 diff 최대 길이
        // OutOfMemoryException 방지를 위해 읽기 단계에서부터 제한
        private const int MAX_DIFF_LENGTH = 8000;

        public PromptService(GitCommandExecutor executor, HttpClient httpClient)
        {
            _executor = executor;
            _httpClient = httpClient;
        }

        public async Task<string> CreateInitialCommitPromptAsync(string repoPath)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("아래는 내 프로젝트의 전체 소스 코드입니다.");
            promptBuilder.AppendLine("이 프로젝트가 어떤 기능을 하는지 분석하고, 프로젝트의 첫 커밋(Initial Commit)에 어울리는 커밋 메시지를 생성해주세요.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("STRICT OUTPUT FORMAT - You MUST follow this exactly:");
            promptBuilder.AppendLine("<title>feat: your title here (max 50 chars)</title>");
            promptBuilder.AppendLine("<body>");
            promptBuilder.AppendLine("- English description line 1");
            promptBuilder.AppendLine("- English description line 2");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("- 한국어 설명 1");
            promptBuilder.AppendLine("- 한국어 설명 2");
            promptBuilder.AppendLine("</body>");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("RULES:");
            promptBuilder.AppendLine("- <title> must be 50 characters or less including the type prefix");
            promptBuilder.AppendLine("- For initial commits, use type 'feat' or 'init'");
            promptBuilder.AppendLine("- Use imperative mood (Add, Fix, Update — not Added, Fixed, Updated)");
            promptBuilder.AppendLine("- No period at end of title");
            promptBuilder.AppendLine("- Body: English bullet points first, then Korean bullet points after a blank line");
            promptBuilder.AppendLine("- Output ONLY the XML tags above. No explanations, no markdown, no code blocks.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("--- 전체 코드 내용 ---");

            try
            {
                var ignoreDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "bin", "obj", ".vs", ".git", "node_modules" };
                var allFiles = Directory.GetFiles(repoPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    bool isIgnored = ignoreDirs.Any(dir =>
                        file.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar));
                    if (isIgnored) continue;

                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (new[] { ".exe", ".dll", ".pdb", ".png", ".jpg", ".ico", ".user", ".suo" }
                        .Contains(ext)) continue;

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

        // PromptService.cs 내 해당 메서드만 교체

        public Task<string> CreateGitignorePromptAsync(string repoPath,
            List<string>? excludedPaths = null)
        {
            var promptBuilder = new StringBuilder();

            try
            {
                var allFiles = Directory.GetFiles(repoPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.Combine(repoPath, ".git")))
                    .ToList();

                var extensionGroups = allFiles
                    .Select(Path.GetExtension)
                    .Where(ext => !string.IsNullOrEmpty(ext))
                    .GroupBy(ext => ext!.ToLower())
                    .OrderByDescending(g => g.Count())
                    .Take(15)
                    .ToDictionary(g => g.Key, g => g.Count());

                var topLevelDirs = Directory.GetDirectories(repoPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith("."))
                    .ToList();

                var rootFiles = Directory.GetFiles(repoPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                var detectedStack = DetectProjectStack(extensionGroups, rootFiles!, topLevelDirs!);

                promptBuilder.AppendLine("아래는 내 프로젝트의 파일 및 폴더 구조에 대한 정보입니다.");
                promptBuilder.AppendLine("이 정보를 바탕으로, 이 프로젝트에 최적화된 .gitignore 파일을 생성해주세요.");
                promptBuilder.AppendLine("반드시 감지된 언어와 프레임워크에 맞는 항목만 포함하고, 관계없는 언어의 항목은 절대 포함하지 마세요.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("--- 프로젝트 정보 ---");
                promptBuilder.AppendLine($"감지된 언어/프레임워크: {detectedStack}");
                promptBuilder.AppendLine($"주요 폴더: {string.Join(", ", topLevelDirs)}");
                promptBuilder.AppendLine($"루트 파일: {string.Join(", ", rootFiles)}");
                promptBuilder.AppendLine($"파일 확장자 (많은 순): {string.Join(", ", extensionGroups.Select(kv => $"{kv.Key}({kv.Value}개)"))}");

                // ▼▼▼ [추가] 사용자가 선택한 제외 경로 명시 ▼▼▼
                if (excludedPaths != null && excludedPaths.Any())
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- 반드시 제외해야 할 항목 (사용자 지정) ---");
                    promptBuilder.AppendLine("아래 항목들은 .gitignore에 반드시 포함되어야 합니다:");
                    foreach (var path in excludedPaths)
                        promptBuilder.AppendLine($"  {path}");
                }
                // ▲▲▲ [추가] 여기까지 ▲▲▲

                // ▼▼▼ [추가] MQL5 프로젝트일 때 Include 폴더 보호 지시 ▼▼▼
                if (detectedStack.Contains("MQL5"))
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- MQL5 프로젝트 특별 지시 ---");
                    promptBuilder.AppendLine("- Include/ 폴더를 .gitignore에 절대 포함하지 마세요!");
                    promptBuilder.AppendLine("- Include/ 폴더 안의 .mqh 파일들은 프로젝트에 반드시 필요한 헤더 파일입니다.");
                    promptBuilder.AppendLine("- MQL5에서 Include/ 폴더는 Node.js의 node_modules/와 다릅니다. 직접 작성한 소스 코드가 들어있습니다.");
                    promptBuilder.AppendLine("- 무시해야 할 것: *.ex4, *.ex5 (컴파일된 바이너리), Logs/, Tester/ 결과, Presets/ 등");
                }
                // ▲▲▲ [추가] 여기까지 ▲▲▲

                promptBuilder.AppendLine();
                promptBuilder.AppendLine("--- 요구사항 ---");
                promptBuilder.AppendLine($"- 위에서 감지된 '{detectedStack}' 프로젝트에 맞는 .gitignore를 생성해주세요.");
                promptBuilder.AppendLine("- 빌드 산출물, 캐시, 의존성 폴더, IDE 설정 파일 등을 포함해주세요.");
                promptBuilder.AppendLine("- 각 섹션에 한국어 주석을 달아주세요.");
                promptBuilder.AppendLine("- .gitignore 내용만 출력하고, 다른 설명은 붙이지 마세요.");
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    $"프로젝트 파일 분석 중 오류가 발생했습니다: {ex.Message}\n\n" +
                    $"분석한 프로젝트에 맞는 .gitignore 파일을 생성해 주세요.");
            }

            return Task.FromResult(promptBuilder.ToString());
        }

        public async Task<string> GenerateGitignoreContentAsync(string repoPath,
            List<string>? excludedPaths = null)
        {
            var prompt = await CreateGitignorePromptAsync(repoPath, excludedPaths);
            return await CallOllamaAsync(prompt);
        }


        private static string DetectProjectStack(
            Dictionary<string, int> extensions,
            List<string> rootFiles,
            List<string> topLevelDirs)
        {
            var detected = new List<string>();

            if (extensions.ContainsKey(".py") ||
                rootFiles.Any(f => f is "requirements.txt" or "setup.py" or "pyproject.toml" or "Pipfile"))
            {
                string pyFramework = "";
                if (rootFiles.Any(f => f == "manage.py")) pyFramework = " (Django)";
                else if (rootFiles.Any(f => f == "app.py")) pyFramework = " (Flask)";
                detected.Add($"Python{pyFramework}");
            }

            if (rootFiles.Any(f => f == "package.json"))
            {
                string jsFramework = "";
                if (rootFiles.Any(f => f is "next.config.js" or "next.config.ts")) jsFramework = " (Next.js)";
                else if (rootFiles.Any(f => f is "vite.config.js" or "vite.config.ts")) jsFramework = " (Vite)";
                else if (rootFiles.Any(f => f == "vue.config.js")) jsFramework = " (Vue.js)";
                else if (rootFiles.Any(f => f == "angular.json")) jsFramework = " (Angular)";

                detected.Add(extensions.ContainsKey(".ts") || extensions.ContainsKey(".tsx")
                    ? $"TypeScript{jsFramework}"
                    : $"JavaScript{jsFramework}");
                detected.Add("Node.js");
            }

            if (extensions.ContainsKey(".cs") ||
                rootFiles.Any(f => f.EndsWith(".sln") || f.EndsWith(".csproj")))
            {
                string dotnetFramework = extensions.ContainsKey(".xaml") ? " (WPF)" : " (.NET)";
                detected.Add($"C#{dotnetFramework}");
            }

            if (extensions.ContainsKey(".java") ||
                rootFiles.Any(f => f is "pom.xml" or "build.gradle" or "build.gradle.kts"))
            {
                string javaFramework = rootFiles.Any(f => f == "pom.xml") ? " (Maven)" : " (Gradle)";
                detected.Add($"Java{javaFramework}");
            }

            if (extensions.ContainsKey(".kt") || extensions.ContainsKey(".kts"))
                detected.Add("Kotlin");
            if (extensions.ContainsKey(".go") || rootFiles.Any(f => f is "go.mod" or "go.sum"))
                detected.Add("Go");
            if (extensions.ContainsKey(".rs") || rootFiles.Any(f => f is "Cargo.toml" or "Cargo.lock"))
                detected.Add("Rust");
            if (extensions.ContainsKey(".rb") || rootFiles.Any(f => f is "Gemfile" or "Rakefile"))
                detected.Add("Ruby");
            if (extensions.ContainsKey(".php") || rootFiles.Any(f => f == "composer.json"))
                detected.Add("PHP");
            if (extensions.ContainsKey(".swift") || rootFiles.Any(f => f == "Package.swift") ||
                topLevelDirs.Any(d => d.EndsWith(".xcodeproj") || d.EndsWith(".xcworkspace")))
                detected.Add("Swift");
            if (extensions.ContainsKey(".c") || extensions.ContainsKey(".cpp") ||
                extensions.ContainsKey(".h") || extensions.ContainsKey(".hpp"))
                detected.Add("C/C++");
            if (extensions.ContainsKey(".dart") || rootFiles.Any(f => f == "pubspec.yaml"))
                detected.Add("Flutter/Dart");
            if (rootFiles.Any(f => f is "docker-compose.yml" or "docker-compose.yaml" or "Dockerfile"))
                detected.Add("Docker");
            if (topLevelDirs.Any(d => d == ".terraform") || rootFiles.Any(f => f.EndsWith(".tf")))
                detected.Add("Terraform");

            // ▼▼▼ [추가] MQL5 프로젝트 감지 ▼▼▼
            if (extensions.ContainsKey(".mq5") || extensions.ContainsKey(".mq4") ||
                extensions.ContainsKey(".mqh") ||
                extensions.ContainsKey(".ex5") || extensions.ContainsKey(".ex4") ||
                topLevelDirs.Any(d => d.Equals("MQL5", StringComparison.OrdinalIgnoreCase)) ||
                topLevelDirs.Any(d => d.Equals("MQL4", StringComparison.OrdinalIgnoreCase)) ||
                topLevelDirs.Any(d => d.Equals("Experts", StringComparison.OrdinalIgnoreCase) ||
                                      d.Equals("Indicators", StringComparison.OrdinalIgnoreCase) ||
                                      d.Equals("Scripts", StringComparison.OrdinalIgnoreCase)) &&
                (extensions.ContainsKey(".mq5") || extensions.ContainsKey(".mq4") || extensions.ContainsKey(".mqh")))
            {
                detected.Add("MQL5 (MetaTrader)");
            }
            // ▲▲▲ [추가] 여기까지 ▲▲▲

            return detected.Any()
                ? string.Join(", ", detected)
                : "알 수 없음 (파일 목록을 참고하여 적절한 .gitignore를 생성해주세요)";
        }

        // ▼▼▼ [핵심 수정] diff를 파일별로 나눠서 크기 제한 후 조합 ▼▼▼
        public async Task<string> GetDiffAsync(string repoPath)
        {
            try
            {
                // 1단계: 변경된 파일 목록만 먼저 가져옴 (크기 작음)
                var fileListResult = await _executor.ExecuteAsync(repoPath, "diff --cached --name-only");
                if (string.IsNullOrWhiteSpace(fileListResult.Output))
                {
                    fileListResult = await _executor.ExecuteAsync(repoPath, "diff --name-only");
                }

                var changedFiles = fileListResult.Output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                Console.WriteLine($"[DEBUG] 변경된 파일 수: {changedFiles.Count}");

                if (changedFiles.Count == 0)
                {
                    // staged/unstaged 모두 없으면 HEAD 기준으로 파일 목록 재시도
                    var headFileList = await _executor.ExecuteAsync(repoPath, "diff --name-only HEAD");
                    changedFiles = headFileList.Output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    if (changedFiles.Count == 0)
                        return "커밋할 변경 사항이 없습니다.";
                }

                // 2단계: 파일별로 diff를 가져와서 MAX_DIFF_LENGTH 안에서 조합
                var resultBuilder = new StringBuilder();
                int totalLength = 0;
                int includedFiles = 0;
                int skippedFiles = 0;

                foreach (var file in changedFiles)
                {
                    if (totalLength >= MAX_DIFF_LENGTH)
                    {
                        skippedFiles++;
                        continue;
                    }

                    // 파일 하나의 diff만 가져옴
                    var fileDiffResult = await _executor.ExecuteAsync(
                        repoPath, $"diff --cached --text -- \"{file}\"");

                    // staged에 없으면 unstaged에서 시도
                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                    {
                        fileDiffResult = await _executor.ExecuteAsync(
                            repoPath, $"diff --text -- \"{file}\"");
                    }

                    // 그래도 없으면 HEAD 기준으로 시도
                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                    {
                        fileDiffResult = await _executor.ExecuteAsync(
                            repoPath, $"diff --text HEAD -- \"{file}\"");
                    }

                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                    {
                        Console.WriteLine($"[DEBUG] {file}: diff 없음, 건너뜀");
                        continue;
                    }

                    string fileDiff = fileDiffResult.Output;

                    // 파일 하나의 diff가 남은 공간보다 크면 앞부분만 자름
                    int remaining = MAX_DIFF_LENGTH - totalLength;
                    if (fileDiff.Length > remaining)
                    {
                        fileDiff = fileDiff[..remaining] +
                                   $"\n... [{file} diff가 너무 길어 잘렸습니다]";
                        skippedFiles++; // 이 파일은 부분만 포함
                    }

                    resultBuilder.AppendLine(fileDiff);
                    totalLength += fileDiff.Length;
                    includedFiles++;

                    Console.WriteLine($"[DEBUG] {file}: {fileDiff.Length}자 포함 (누적: {totalLength}자)");
                }

                if (skippedFiles > 0)
                {
                    resultBuilder.AppendLine(
                        $"\n[주의: 변경 파일 {changedFiles.Count}개 중 " +
                        $"{includedFiles}개만 분석에 포함되었습니다. " +
                        $"나머지 {skippedFiles}개는 크기 제한으로 제외되었습니다.]");
                }

                string finalDiff = resultBuilder.ToString().Trim();
                Console.WriteLine($"[DEBUG] 최종 diff 길이: {finalDiff.Length}자");

                return string.IsNullOrWhiteSpace(finalDiff)
                    ? "커밋할 변경 사항이 없습니다."
                    : finalDiff;
            }
            catch (OutOfMemoryException)
            {
                // OutOfMemoryException 전용 처리
                Console.WriteLine("[ERROR] OutOfMemoryException 발생 — diff가 너무 큼");
                return await GetDiffSummaryOnlyAsync(repoPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetDiffAsync 오류: {ex.Message}");
                return $"diff 가져오기 중 오류 발생: {ex.Message}";
            }
        }

        // ▼▼▼ [추가] OutOfMemoryException 발생 시 최후 수단 — 파일 목록과 통계만 반환 ▼▼▼
        private async Task<string> GetDiffSummaryOnlyAsync(string repoPath)
        {
            try
            {
                Console.WriteLine("[DEBUG] diff 요약만 가져오는 모드로 전환");

                // --stat은 숫자 통계만 반환하므로 매우 작음
                var statResult = await _executor.ExecuteAsync(repoPath, "diff --cached --stat");
                if (string.IsNullOrWhiteSpace(statResult.Output))
                    statResult = await _executor.ExecuteAsync(repoPath, "diff --stat HEAD");

                var summary = new StringBuilder();
                summary.AppendLine("[변경 사항이 너무 커서 통계 요약만 표시합니다]");
                summary.AppendLine();
                summary.AppendLine(statResult.Output);

                return summary.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"변경 사항 요약 가져오기 실패: {ex.Message}";
            }
        }
        // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲

        public string CreateCommitPrompt(string diffContent)
        {
            return
$@"You are a Git commit message generator. Analyze the diff below and generate a commit message.

STRICT OUTPUT FORMAT - You MUST follow this exactly:
<title>feat: your title here (max 50 chars)</title>
<body>
- English description line 1
- English description line 2

- 한국어 설명 1
- 한국어 설명 2
</body>

RULES:
- <title> must be 50 characters or less including the type prefix (feat/fix/refactor/docs/style/test/chore)
- Use imperative mood (Add, Fix, Update — not Added, Fixed, Updated)
- No period at end of title
- Body: English bullet points first, then Korean bullet points after a blank line
- Output ONLY the XML tags above. No explanations, no markdown, no code blocks.

diff:
{diffContent}
";
        }

        private async Task<string> CallOllamaAsync(string prompt, string model = OLLAMA_MODEL_NAME)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Ollama API 호출 시작. 모델: {model}, 프롬프트 길이: {prompt.Length}");

                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/generate", httpContent);

                Console.WriteLine($"[DEBUG] Ollama API 응답 상태 코드: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR] Ollama API 요청 실패: {response.StatusCode} - {errorContent}");
                    throw new Exception($"Ollama API 요청 실패: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Ollama Raw Response (first 500 chars): " +
                                  $"{responseContent[..Math.Min(500, responseContent.Length)]}...");

                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(
                    responseContent, deserializeOptions);

                Console.WriteLine($"[DEBUG] Response 길이: {ollamaResponse?.Response?.Length ?? 0}");

                return ollamaResponse?.Response ?? "Ollama 응답에서 메시지를 찾을 수 없습니다.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] Ollama API 호출 중 오류: {ex.Message}");
                Console.WriteLine($"[EXCEPTION] Stack Trace: {ex.StackTrace}");
                return $"Ollama API 호출 중 오류 발생: {ex.Message}";
            }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; } = string.Empty;
        }

        public async Task<string> GenerateCommitMessageAsync(string repoPath)
        {
            Console.WriteLine($"[DEBUG] GenerateCommitMessageAsync 시작. repoPath: {repoPath}");

            string diffContent;
            try
            {
                diffContent = await GetDiffAsync(repoPath);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("[ERROR] GenerateCommitMessageAsync — OutOfMemoryException");
                diffContent = await GetDiffSummaryOnlyAsync(repoPath);
            }

            Console.WriteLine($"[DEBUG] diff 길이: {diffContent.Length}");

            if (string.IsNullOrWhiteSpace(diffContent) || diffContent.Contains("변경 사항이 없습니다"))
            {
                Console.WriteLine("[DEBUG] diff 없음. early return.");
                return "변경 사항이 없어 커밋 메시지를 생성할 수 없습니다.";
            }

            var prompt = CreateCommitPrompt(diffContent);
            Console.WriteLine($"[DEBUG] 프롬프트 길이: {prompt.Length}");

            var aiResponse = await CallOllamaAsync(prompt);
            Console.WriteLine($"[DEBUG] AI 응답 길이: {aiResponse.Length}");
            return aiResponse;
        }

        // ▼▼▼ [수정] Initial Commit 메시지도 XML 태그 형식으로 생성하도록 변경 ▼▼▼
        public async Task<string> GenerateInitialCommitMessageAsync(string repoPath)
        {
            var prompt = await CreateInitialCommitPromptAsync(repoPath);
            return await CallOllamaAsync(prompt);
        }
        // ▲▲▲ [수정] 여기까지 ▲▲▲

        public async Task<string> GenerateGitignoreContentAsync(string repoPath)
        {
            var prompt = await CreateGitignorePromptAsync(repoPath);
            return await CallOllamaAsync(prompt);
        }
    }
}
