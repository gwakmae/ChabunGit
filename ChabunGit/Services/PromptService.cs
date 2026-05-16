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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly GitCommandExecutor _executor;
        private readonly HttpClient _httpClient;

        private const string OLLAMA_MODEL_NAME = "gemma4-unsloth:latest";
        private const int MAX_DIFF_LENGTH = 8000;
        private const int MAX_INITIAL_PROMPT_LENGTH = 30000;
        private const int MAX_INITIAL_FILE_CHARS = 3000;
        private const int MAX_INITIAL_FILES = 80;
        private const long MAX_INITIAL_FILE_BYTES = 512 * 1024;

        public PromptService(GitCommandExecutor executor, HttpClient httpClient)
        {
            _executor = executor;
            _httpClient = httpClient;
        }

        public async Task<string> CreateInitialCommitPromptAsync(string repoPath)
        {
            return await Task.Run(async () =>
            {
                var promptBuilder = new StringBuilder();

                promptBuilder.AppendLine("아래는 내 프로젝트의 핵심 파일 구조와 일부 소스 코드입니다.");
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

                try
                {
                    var enumerationOptions = new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        ReturnSpecialDirectories = false
                    };

                    var allFiles = Directory.EnumerateFiles(repoPath, "*", enumerationOptions)
                        .Where(file => ShouldIncludeInitialCommitFile(repoPath, file))
                        .OrderBy(GetInitialCommitFilePriority)
                        .ThenBy(file => Path.GetRelativePath(repoPath, file))
                        .Take(MAX_INITIAL_FILES)
                        .ToList();

                    promptBuilder.AppendLine("--- 프로젝트 파일 목록 요약 ---");

                    foreach (var file in allFiles)
                    {
                        string relativePath = Path.GetRelativePath(repoPath, file).Replace("\\", "/");
                        promptBuilder.AppendLine(relativePath);
                    }

                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- 핵심 코드 일부 ---");

                    int includedFiles = 0;
                    int skippedByLength = 0;

                    foreach (var file in allFiles)
                    {
                        if (promptBuilder.Length >= MAX_INITIAL_PROMPT_LENGTH)
                        {
                            skippedByLength++;
                            continue;
                        }

                        string relativePath = Path.GetRelativePath(repoPath, file).Replace("\\", "/");

                        string content;
                        try
                        {
                            content = await File.ReadAllTextAsync(file);
                        }
                        catch
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(content))
                            continue;

                        if (content.Length > MAX_INITIAL_FILE_CHARS)
                        {
                            content = content[..MAX_INITIAL_FILE_CHARS] +
                                      "\n... [파일 내용이 길어 일부만 포함되었습니다]";
                        }

                        string section =
                            $"\n--- File: {relativePath} ---\n" +
                            content +
                            "\n";

                        int remaining = MAX_INITIAL_PROMPT_LENGTH - promptBuilder.Length;

                        if (section.Length > remaining)
                        {
                            if (remaining > 200)
                            {
                                promptBuilder.AppendLine(section[..remaining]);
                                promptBuilder.AppendLine("\n... [전체 프롬프트 길이 제한으로 이후 내용은 생략되었습니다]");
                            }

                            skippedByLength++;
                            break;
                        }

                        promptBuilder.AppendLine(section);
                        includedFiles++;
                    }

                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- 분석 참고 ---");
                    promptBuilder.AppendLine($"분석에 포함된 파일 수: {includedFiles}");
                    promptBuilder.AppendLine($"크기 제한으로 제외되거나 잘린 파일 수: {skippedByLength}");
                    promptBuilder.AppendLine("위 정보만 바탕으로 프로젝트의 목적을 요약하고 Initial Commit 메시지를 생성하세요.");
                }
                catch (Exception ex)
                {
                    promptBuilder.AppendLine($"\n파일을 읽는 중 오류 발생: {ex.Message}");
                }

                return promptBuilder.ToString();
            });
        }

        private static bool ShouldIncludeInitialCommitFile(string repoPath, string file)
        {
            try
            {
                string relativePath = Path.GetRelativePath(repoPath, file);

                var ignoredDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".git", ".vs", ".idea", ".vscode",
                    "bin", "obj", "node_modules", "packages",
                    "dist", "build", ".next", ".nuxt",
                    "coverage", "target", "out",
                    "Debug", "Release", "x64", "x86",
                    "venv", ".venv", "__pycache__",
                    ".pytest_cache", ".mypy_cache"
                };

                var pathParts = relativePath.Split(
                    new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries);

                if (pathParts.Any(part => ignoredDirectoryNames.Contains(part)))
                    return false;

                string extension = Path.GetExtension(file).ToLowerInvariant();

                var ignoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".exe", ".dll", ".pdb",
                    ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".svg",
                    ".mp4", ".mp3", ".wav", ".avi", ".mov",
                    ".zip", ".7z", ".rar", ".tar", ".gz",
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
                    ".user", ".suo", ".cache",
                    ".db", ".sqlite", ".sqlite3",
                    ".log", ".tmp", ".bak",
                    ".onnx", ".bin", ".pt", ".pth", ".safetensors"
                };

                if (ignoredExtensions.Contains(extension))
                    return false;

                var info = new FileInfo(file);
                if (!info.Exists) return false;
                if (info.Length > MAX_INITIAL_FILE_BYTES) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int GetInitialCommitFilePriority(string file)
        {
            string name = Path.GetFileName(file).ToLowerInvariant();
            string extension = Path.GetExtension(file).ToLowerInvariant();

            if (name is "readme.md" or "license" or ".gitignore" or "app.xaml" or "app.xaml.cs") return 0;
            if (name.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)) return 0;
            if (name is "package.json" or "tsconfig.json" or "vite.config.js" or "vite.config.ts" or
                "next.config.js" or "next.config.ts" or "angular.json" or "vue.config.js") return 0;
            if (name is "requirements.txt" or "pyproject.toml" or "setup.py" or "pipfile" or "manage.py") return 0;
            if (name is "cargo.toml" or "go.mod" or "composer.json" or "pom.xml" or
                "build.gradle" or "build.gradle.kts" or "dockerfile" or "docker-compose.yml" or
                "docker-compose.yaml") return 0;

            if (extension is ".cs" or ".xaml" or ".py" or ".js" or ".jsx" or ".ts" or ".tsx" or
                ".java" or ".kt" or ".go" or ".rs" or ".php" or ".rb" or ".swift" or
                ".cpp" or ".c" or ".h" or ".hpp" or ".mq4" or ".mq5" or ".mqh") return 1;

            if (extension is ".json" or ".xml" or ".yml" or ".yaml" or ".md" or ".txt" or ".config") return 2;

            return 3;
        }

        public Task<string> CreateGitignorePromptAsync(string repoPath, List<string>? excludedPaths = null)
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

                if (excludedPaths != null && excludedPaths.Any())
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- 반드시 제외해야 할 항목 (사용자 지정) ---");
                    promptBuilder.AppendLine("아래 항목들은 .gitignore에 반드시 포함되어야 합니다:");
                    foreach (var path in excludedPaths)
                        promptBuilder.AppendLine($"  {path}");
                }

                if (detectedStack.Contains("MQL5"))
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- MQL5 프로젝트 특별 지시 ---");
                    promptBuilder.AppendLine("- Include/ 폴더를 .gitignore에 절대 포함하지 마세요!");
                    promptBuilder.AppendLine("- Include/ 폴더 안의 .mqh 파일들은 프로젝트에 반드시 필요한 헤더 파일입니다.");
                    promptBuilder.AppendLine("- 무시해야 할 것: *.ex4, *.ex5 (컴파일된 바이너리), Logs/, Tester/ 결과, Presets/ 등");
                }

                promptBuilder.AppendLine();
                promptBuilder.AppendLine("--- 요구사항 ---");
                promptBuilder.AppendLine($"- 위에서 감지된 '{detectedStack}' 프로젝트에 맞는 .gitignore를 생성해주세요.");
                promptBuilder.AppendLine("- 빌드 산출물, 캐시, 의존성 폴더, IDE 설정 파일 등을 포함해주세요.");
                promptBuilder.AppendLine("- 각 섹션에 한국어 주석을 달아주세요.");
                promptBuilder.AppendLine("- .gitignore 내용만 출력하고, 다른 설명은 붙이지 마세요.");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"프로젝트 파일 분석 중 오류가 발생했습니다: {ex.Message}\n\n분석한 프로젝트에 맞는 .gitignore 파일을 생성해 주세요.");
            }

            return Task.FromResult(promptBuilder.ToString());
        }

        public async Task<string> GenerateGitignoreContentAsync(string repoPath, List<string>? excludedPaths = null)
        {
            var prompt = await CreateGitignorePromptAsync(repoPath, excludedPaths);
            return await CallOllamaAsync(prompt);
        }

        private static string DetectProjectStack(Dictionary<string, int> extensions, List<string> rootFiles, List<string> topLevelDirs)
        {
            var detected = new List<string>();

            if (extensions.ContainsKey(".py") || rootFiles.Any(f => f is "requirements.txt" or "setup.py" or "pyproject.toml" or "Pipfile"))
                detected.Add("Python");

            if (rootFiles.Any(f => f == "package.json"))
            {
                detected.Add(extensions.ContainsKey(".ts") || extensions.ContainsKey(".tsx") ? "TypeScript" : "JavaScript");
                detected.Add("Node.js");
            }

            if (extensions.ContainsKey(".cs") || rootFiles.Any(f => f.EndsWith(".sln") || f.EndsWith(".csproj")))
                detected.Add(extensions.ContainsKey(".xaml") ? "C# (WPF)" : "C# (.NET)");

            if (extensions.ContainsKey(".java") || rootFiles.Any(f => f is "pom.xml" or "build.gradle" or "build.gradle.kts"))
                detected.Add("Java");

            if (extensions.ContainsKey(".kt") || extensions.ContainsKey(".kts")) detected.Add("Kotlin");
            if (extensions.ContainsKey(".go") || rootFiles.Any(f => f is "go.mod" or "go.sum")) detected.Add("Go");
            if (extensions.ContainsKey(".rs") || rootFiles.Any(f => f is "Cargo.toml" or "Cargo.lock")) detected.Add("Rust");
            if (extensions.ContainsKey(".rb") || rootFiles.Any(f => f is "Gemfile" or "Rakefile")) detected.Add("Ruby");
            if (extensions.ContainsKey(".php") || rootFiles.Any(f => f == "composer.json")) detected.Add("PHP");
            if (extensions.ContainsKey(".swift") || rootFiles.Any(f => f == "Package.swift")) detected.Add("Swift");
            if (extensions.ContainsKey(".c") || extensions.ContainsKey(".cpp")) detected.Add("C/C++");
            if (extensions.ContainsKey(".dart") || rootFiles.Any(f => f == "pubspec.yaml")) detected.Add("Flutter/Dart");
            if (rootFiles.Any(f => f is "docker-compose.yml" or "docker-compose.yaml" or "Dockerfile")) detected.Add("Docker");

            if (extensions.ContainsKey(".mq5") || extensions.ContainsKey(".mq4") || extensions.ContainsKey(".mqh"))
                detected.Add("MQL5 (MetaTrader)");

            return detected.Any() ? string.Join(", ", detected) : "알 수 없음";
        }

        public async Task<string> GetDiffAsync(string repoPath)
        {
            try
            {
                var fileListResult = await _executor.ExecuteAsync(repoPath, "diff --cached --name-only");
                if (string.IsNullOrWhiteSpace(fileListResult.Output))
                    fileListResult = await _executor.ExecuteAsync(repoPath, "diff --name-only");

                var changedFiles = fileListResult.Output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (changedFiles.Count == 0)
                {
                    var headFileList = await _executor.ExecuteAsync(repoPath, "diff --name-only HEAD");
                    changedFiles = headFileList.Output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    if (changedFiles.Count == 0) return "커밋할 변경 사항이 없습니다.";
                }

                var resultBuilder = new StringBuilder();
                int totalLength = 0;

                foreach (var file in changedFiles)
                {
                    if (totalLength >= MAX_DIFF_LENGTH) break;

                    var fileDiffResult = await _executor.ExecuteAsync(repoPath, $"diff --cached --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                        fileDiffResult = await _executor.ExecuteAsync(repoPath, $"diff --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                        fileDiffResult = await _executor.ExecuteAsync(repoPath, $"diff --text HEAD -- \"{file}\"");

                    if (!string.IsNullOrWhiteSpace(fileDiffResult.Output))
                    {
                        string fileDiff = fileDiffResult.Output;
                        if (fileDiff.Length > (MAX_DIFF_LENGTH - totalLength))
                            fileDiff = fileDiff[..(MAX_DIFF_LENGTH - totalLength)] + "\n... [diff 잘림]";

                        resultBuilder.AppendLine(fileDiff);
                        totalLength += fileDiff.Length;
                    }
                }

                string finalDiff = resultBuilder.ToString().Trim();
                return string.IsNullOrWhiteSpace(finalDiff) ? "커밋할 변경 사항이 없습니다." : finalDiff;
            }
            catch (Exception ex)
            {
                return $"diff 가져오기 중 오류 발생: {ex.Message}";
            }
        }

        // ▼▼▼ 파일별로 diff를 가져오는 Map-Reduce 용 헬퍼 ▼▼▼
        private async Task<List<(string FilePath, string Diff)>> GetDiffPerFileAsync(string repoPath)
        {
            var result = new List<(string, string)>();

            try
            {
                var fileListResult = await _executor.ExecuteAsync(repoPath, "diff --cached --name-only");
                if (string.IsNullOrWhiteSpace(fileListResult.Output))
                    fileListResult = await _executor.ExecuteAsync(repoPath, "diff --name-only");

                var changedFiles = fileListResult.Output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (changedFiles.Count == 0)
                {
                    var headFileList = await _executor.ExecuteAsync(repoPath, "diff --name-only HEAD");
                    changedFiles = headFileList.Output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                const int MAX_PER_FILE_DIFF = 3000;

                foreach (var file in changedFiles)
                {
                    var diffResult = await _executor.ExecuteAsync(repoPath, $"diff --cached --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(diffResult.Output))
                        diffResult = await _executor.ExecuteAsync(repoPath, $"diff --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(diffResult.Output))
                        diffResult = await _executor.ExecuteAsync(repoPath, $"diff --text HEAD -- \"{file}\"");

                    if (string.IsNullOrWhiteSpace(diffResult.Output))
                        continue;

                    string diff = diffResult.Output;
                    if (diff.Length > MAX_PER_FILE_DIFF)
                        diff = diff[..MAX_PER_FILE_DIFF] + "\n... [이 파일의 diff가 길어 일부만 포함됨]";

                    result.Add((file, diff));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetDiffPerFileAsync: {ex.Message}");
            }

            return result;
        }

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

        private async Task<string> CallOllamaAsync(string prompt, string model = OLLAMA_MODEL_NAME, int maxTokens = 700)
        {
            try
            {
                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.2,
                        num_predict = maxTokens,
                        num_ctx = 8192
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/generate", httpContent);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Ollama API 요청 실패: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent, deserializeOptions);

                return ollamaResponse?.Response ?? "Ollama 응답에서 메시지를 찾을 수 없습니다.";
            }
            catch (Exception ex)
            {
                return $"Ollama API 호출 중 오류 발생: {ex.Message}";
            }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; } = string.Empty;
        }

        private static string CleanTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "chore: update project";
            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);

            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim().Trim('`', '"', '*', '-', ' '))
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToList();

            var typed = lines.FirstOrDefault(l =>
                Regex.IsMatch(l, @"^(feat|fix|refactor|docs|style|test|chore|perf|ci|build|init)\s*(\(.+\))?\s*:",
                              RegexOptions.IgnoreCase));

            string title = typed ?? lines.FirstOrDefault() ?? "chore: update project";
            title = Regex.Replace(title, @"^(here\s+is\s+the\s+(commit\s+)?title\s*[:：]?\s*)", "", RegexOptions.IgnoreCase).Trim();
            if (title.Length > 50) title = title[..50];

            return title.TrimEnd('.', ' ', '\t');
        }

        private static string CleanBody(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);
            raw = Regex.Replace(raw, @"^#+\s+.*$", "", RegexOptions.Multiline);
            return raw.Trim('`', '"', '\r', '\n', ' ');
        }

        // ▼▼▼ 파일 1개 한줄 요약 정리 헬퍼 ▼▼▼
        private static string CleanSingleLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);

            var firstLine = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                               .Select(l => l.Trim().Trim('`', '"', '*', '-', '•', ' '))
                               .FirstOrDefault(l => l.Length > 3);

            if (string.IsNullOrWhiteSpace(firstLine)) return string.Empty;

            firstLine = Regex.Replace(firstLine, @"^(here\s+is\s+.*?[:：]\s*|summary\s*[:：]\s*|change\s*[:：]\s*)", "", RegexOptions.IgnoreCase).Trim();
            if (firstLine.Length > 120) firstLine = firstLine[..120];

            return firstLine;
        }

        // ▼▼▼ Map-Reduce & Progress 패턴이 적용된 GenerateCommitMessageAsync ▼▼▼
        public async Task<string> GenerateCommitMessageAsync(string repoPath, IProgress<string>? progress = null)
        {
            progress?.Report("🔍 변경된 파일 목록을 가져오는 중...");

            var perFileDiffs = await GetDiffPerFileAsync(repoPath);

            if (perFileDiffs.Count == 0)
            {
                progress?.Report("⚠️ 변경 사항이 없습니다.");
                return "변경 사항이 없어 커밋 메시지를 생성할 수 없습니다.";
            }

            int totalFiles = perFileDiffs.Count;
            progress?.Report($"📂 총 {totalFiles}개 파일 변경 감지. 분석을 시작합니다.");

            // 1단계 (Map): 파일별 한 줄 요약 누적
            var fileSummaries = new List<string>();
            var startTime = DateTime.Now;
            int processedCount = 0;

            foreach (var (filePath, diff) in perFileDiffs)
            {
                processedCount++;

                string etaText = "";
                if (processedCount > 1)
                {
                    var elapsed = DateTime.Now - startTime;
                    var avgPerFile = elapsed.TotalSeconds / (processedCount - 1);
                    var remainingFiles = totalFiles - processedCount + 1;
                    var etaSeconds = avgPerFile * remainingFiles;
                    etaText = etaSeconds > 60
                        ? $" (예상 남은 시간: 약 {etaSeconds / 60:F1}분)"
                        : $" (예상 남은 시간: 약 {etaSeconds:F0}초)";
                }

                progress?.Report($"📝 [{processedCount}/{totalFiles}] 분석 중: {filePath}{etaText}");

                string mapPrompt =
$@"You are analyzing a single file's diff. Output ONLY one short English line describing what changed.
RULES:
- Format: '<filename>: <what changed in one short sentence>'
- Max 100 characters.
- Imperative mood (Add, Fix, Update, Remove).
- No bullets, no markdown, no quotes, no explanation.
- ONLY one line.

FILE: {filePath}
DIFF:
{diff}";

                string rawSummary = await CallOllamaAsync(mapPrompt, maxTokens: 80);
                string summary = CleanSingleLine(rawSummary);

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    fileSummaries.Add($"- {summary}");
                    progress?.Report($"    ✅ {summary}");
                }
                else
                {
                    fileSummaries.Add($"- {filePath}: modified");
                    progress?.Report($"    ⚠️ 빈 응답 → 파일명만 기록: {filePath}");
                }
            }

            string accumulatedSummary = string.Join("\n", fileSummaries);
            var totalElapsed = (DateTime.Now - startTime).TotalSeconds;
            progress?.Report($"✅ Map 단계 완료. {totalFiles}개 파일 요약 ({totalElapsed:F1}초 소요)");

            // 2단계 (Reduce-Title): 제목 생성
            progress?.Report("🎯 [Reduce 1/2] 누적 요약을 바탕으로 커밋 제목 생성 중...");

            string titlePrompt =
$@"Below is a list of file changes in this commit. Generate ONLY the commit title.
RULES:
- Format: <type>: <description> (type = feat|fix|refactor|docs|style|test|chore|perf)
- Max 50 characters total.
- Imperative mood. No period. No quotes. No markdown. No explanation.
- Output ONLY one single line.

CHANGES:
{accumulatedSummary}";

            string rawTitle = await CallOllamaAsync(titlePrompt, maxTokens: 100);
            string title = CleanTitle(rawTitle);
            progress?.Report($"    ✅ 제목 생성: {title}");

            // 3단계 (Reduce-Body): 본문 생성
            progress?.Report("📄 [Reduce 2/2] 누적 요약을 바탕으로 커밋 본문 생성 중...");

            string bodyPrompt =
$@"Below is a list of file changes in this commit, plus the commit title.
Write the commit body that summarizes the changes.

TITLE: {title}

CHANGES:
{accumulatedSummary}

RULES:
- Write 3-5 bullet points in English starting with '-'.
- Then a blank line.
- Then write 3-5 bullet points in Korean starting with '-'.
- Group related changes together, do NOT just copy file names.
- No title repetition. No XML tags. No markdown headers. No code blocks.
- Output ONLY the bullets.";

            string rawBody = await CallOllamaAsync(bodyPrompt, maxTokens: 600);
            string body = CleanBody(rawBody);

            if (string.IsNullOrWhiteSpace(body))
            {
                progress?.Report("⚠️ 본문 생성 실패 → 누적 요약을 본문으로 대체");
                body = "Changes summary:\n" + accumulatedSummary;
            }
            else
            {
                progress?.Report($"    ✅ 본문 생성 완료 ({body.Length}자)");
            }

            progress?.Report($"🎉 전체 완료! 총 소요 시간: {(DateTime.Now - startTime).TotalSeconds:F1}초");

            return $"<title>{title}</title>\n<body>\n{body}\n</body>";
        }

        // ▼▼▼ Progress 패턴이 적용된 GenerateInitialCommitMessageAsync ▼▼▼
        public async Task<string> GenerateInitialCommitMessageAsync(string repoPath, IProgress<string>? progress = null)
        {
            progress?.Report("📂 프로젝트 구조를 분석하는 중...");

            string projectContext = await CreateInitialCommitPromptAsync(repoPath);
            progress?.Report($"✅ 프로젝트 컨텍스트 수집 완료 ({projectContext.Length}자)");

            if (string.IsNullOrWhiteSpace(projectContext))
                return "Initial Commit 메시지를 생성할 수 없습니다. 분석할 프로젝트 정보가 없습니다.";

            progress?.Report("🎯 [1/2] Initial Commit 제목 생성 중...");
            string titlePrompt =
$@"You are analyzing a project's first commit. Read the project structure below and output ONLY the commit title.
RULES:
- Format: feat: <short project description> OR init: <project name>
- Max 50 characters total.
- Imperative mood. No period at end. No quotes. No markdown. No explanation.
- Output ONLY one single line.

PROJECT CONTEXT:
{projectContext}";

            string rawTitle = await CallOllamaAsync(titlePrompt, maxTokens: 150);
            string title = CleanTitle(rawTitle);
            progress?.Report($"    ✅ 제목: {title}");

            progress?.Report("📄 [2/2] Initial Commit 본문 생성 중...");
            string bodyPrompt =
$@"You are writing the body of a project's first commit message.
TITLE: {title}

RULES:
- 2-4 English bullets starting with '-' describing main features/structure.
- Then a blank line.
- 2-4 Korean bullets starting with '-'.
- No title repetition. No XML tags. No markdown headers. No code blocks.
- Output ONLY the bullets.

PROJECT CONTEXT:
{projectContext}";

            string rawBody = await CallOllamaAsync(bodyPrompt, maxTokens: 600);
            string body = CleanBody(rawBody);

            progress?.Report("🎉 Initial Commit 메시지 생성 완료!");

            return $"<title>{title}</title>\n<body>\n{body}\n</body>";
        }
    }
}