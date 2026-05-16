using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptBuilder
    {
        private const int MaxInitialPromptLength = 30000;
        private const int MaxInitialFileChars = 3000;
        private const int MaxInitialFiles = 80;
        private const long MaxInitialFileBytes = 512 * 1024;

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
                    var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, ReturnSpecialDirectories = false };
                    var allFiles = Directory.EnumerateFiles(repoPath, "*", enumerationOptions)
                        .Where(file => ShouldIncludeInitialCommitFile(repoPath, file))
                        .OrderBy(GetInitialCommitFilePriority)
                        .ThenBy(file => Path.GetRelativePath(repoPath, file))
                        .Take(MaxInitialFiles)
                        .ToList();

                    promptBuilder.AppendLine("--- 프로젝트 파일 목록 요약 ---");
                    foreach (var file in allFiles) promptBuilder.AppendLine(Path.GetRelativePath(repoPath, file).Replace("\\", "/"));
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("--- 핵심 코드 일부 ---");

                    int includedFiles = 0, skippedByLength = 0;
                    foreach (var file in allFiles)
                    {
                        if (promptBuilder.Length >= MaxInitialPromptLength) { skippedByLength++; continue; }
                        string relativePath = Path.GetRelativePath(repoPath, file).Replace("\\", "/");
                        string content;
                        try { content = await File.ReadAllTextAsync(file); } catch { continue; }
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        if (content.Length > MaxInitialFileChars)
                            content = content[..MaxInitialFileChars] + "\n... [파일 내용이 길어 일부만 포함되었습니다]";

                        string section = $"\n--- File: {relativePath} ---\n" + content + "\n";
                        int remaining = MaxInitialPromptLength - promptBuilder.Length;
                        if (section.Length > remaining)
                        {
                            if (remaining > 200)
                            {
                                promptBuilder.AppendLine(section[..remaining]);
                                promptBuilder.AppendLine("\n... [전체 프롬프트 길이 제한으로 이후 내용은 생략되었습니다]");
                            }
                            skippedByLength++; break;
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

        public Task<string> CreateGitignorePromptAsync(string repoPath, List<string>? excludedPaths = null)
        {
            var promptBuilder = new StringBuilder();
            try
            {
                var allFiles = Directory.GetFiles(repoPath, "*.*", SearchOption.AllDirectories).Where(f => !f.Contains(Path.Combine(repoPath, ".git"))).ToList();
                var extensionGroups = allFiles.Select(Path.GetExtension).Where(ext => !string.IsNullOrEmpty(ext))
                    .GroupBy(ext => ext!.ToLower()).OrderByDescending(g => g.Count()).Take(15)
                    .ToDictionary(g => g.Key, g => g.Count());
                var topLevelDirs = Directory.GetDirectories(repoPath).Select(Path.GetFileName).Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith(".")).ToList();
                var rootFiles = Directory.GetFiles(repoPath).Select(Path.GetFileName).Where(name => !string.IsNullOrEmpty(name)).ToList();
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
                    foreach (var path in excludedPaths) promptBuilder.AppendLine($"  {path}");
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
                return Task.FromResult($"프로젝트 파일 분석 중 오류가 발생했습니다: {ex.Message}\n분석한 프로젝트에 맞는 .gitignore 파일을 생성해 주세요.");
            }
            return Task.FromResult(promptBuilder.ToString());
        }

        public string CreateCommitPrompt(string diffContent) => $@"You are a Git commit message generator. Analyze the diff below and generate a commit message.
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

        private static bool ShouldIncludeInitialCommitFile(string repoPath, string file)
        {
            try
            {
                string relativePath = Path.GetRelativePath(repoPath, file);
                var ignoredDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".git", ".vs", ".idea", ".vscode", "bin", "obj", "node_modules", "packages", "dist", "build", ".next", ".nuxt", "coverage", "target", "out", "Debug", "Release", "x64", "x86", "venv", ".venv", "__pycache__", ".pytest_cache", ".mypy_cache" };
                var pathParts = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (pathParts.Any(part => ignoredDirectoryNames.Contains(part))) return false;

                string extension = Path.GetExtension(file).ToLowerInvariant();
                var ignoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".exe", ".dll", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".svg", ".mp4", ".mp3", ".wav", ".avi", ".mov", ".zip", ".7z", ".rar", ".tar", ".gz", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".user", ".suo", ".cache", ".db", ".sqlite", ".sqlite3", ".log", ".tmp", ".bak", ".onnx", ".bin", ".pt", ".pth", ".safetensors" };
                if (ignoredExtensions.Contains(extension)) return false;

                var info = new FileInfo(file);
                return info.Exists && info.Length <= MaxInitialFileBytes;
            }
            catch { return false; }
        }

        private static int GetInitialCommitFilePriority(string file)
        {
            string name = Path.GetFileName(file).ToLowerInvariant();
            string extension = Path.GetExtension(file).ToLowerInvariant();
            if (name is "readme.md" or "license" or ".gitignore" or "app.xaml" or "app.xaml.cs") return 0;
            if (name.EndsWith(".sln") || name.EndsWith(".csproj") || name.EndsWith(".vbproj") || name.EndsWith(".fsproj")) return 0;
            if (name is "package.json" or "tsconfig.json" or "vite.config.js" or "vite.config.ts" or "next.config.js" or "next.config.ts" or "angular.json" or "vue.config.js") return 0;
            if (name is "requirements.txt" or "pyproject.toml" or "setup.py" or "pipfile" or "manage.py") return 0;
            if (name is "cargo.toml" or "go.mod" or "composer.json" or "pom.xml" or "build.gradle" or "build.gradle.kts" or "dockerfile" or "docker-compose.yml" or "docker-compose.yaml") return 0;
            if (extension is ".cs" or ".xaml" or ".py" or ".js" or ".jsx" or ".ts" or ".tsx" or ".java" or ".kt" or ".go" or ".rs" or ".php" or ".rb" or ".swift" or ".cpp" or ".c" or ".h" or ".hpp" or ".mq4" or ".mq5" or ".mqh") return 1;
            if (extension is ".json" or ".xml" or ".yml" or ".yaml" or ".md" or ".txt" or ".config") return 2;
            return 3;
        }

        private static string DetectProjectStack(Dictionary<string, int> extensions, List<string> rootFiles, List<string> topLevelDirs)
        {
            var detected = new List<string>();
            if (extensions.ContainsKey(".py") || rootFiles.Any(f => f is "requirements.txt" or "setup.py" or "pyproject.toml" or "Pipfile")) detected.Add("Python");
            if (rootFiles.Any(f => f == "package.json")) { detected.Add(extensions.ContainsKey(".ts") || extensions.ContainsKey(".tsx") ? "TypeScript" : "JavaScript"); detected.Add("Node.js"); }
            if (extensions.ContainsKey(".cs") || rootFiles.Any(f => f.EndsWith(".sln") || f.EndsWith(".csproj"))) detected.Add(extensions.ContainsKey(".xaml") ? "C# (WPF)" : "C# (.NET)");
            if (extensions.ContainsKey(".java") || rootFiles.Any(f => f is "pom.xml" or "build.gradle" or "build.gradle.kts")) detected.Add("Java");
            if (extensions.ContainsKey(".kt") || extensions.ContainsKey(".kts")) detected.Add("Kotlin");
            if (extensions.ContainsKey(".go") || rootFiles.Any(f => f is "go.mod" or "go.sum")) detected.Add("Go");
            if (extensions.ContainsKey(".rs") || rootFiles.Any(f => f is "Cargo.toml" or "Cargo.lock")) detected.Add("Rust");
            if (extensions.ContainsKey(".rb") || rootFiles.Any(f => f is "Gemfile" or "Rakefile")) detected.Add("Ruby");
            if (extensions.ContainsKey(".php") || rootFiles.Any(f => f == "composer.json")) detected.Add("PHP");
            if (extensions.ContainsKey(".swift") || rootFiles.Any(f => f == "Package.swift")) detected.Add("Swift");
            if (extensions.ContainsKey(".c") || extensions.ContainsKey(".cpp")) detected.Add("C/C++");
            if (extensions.ContainsKey(".dart") || rootFiles.Any(f => f == "pubspec.yaml")) detected.Add("Flutter/Dart");
            if (rootFiles.Any(f => f is "docker-compose.yml" or "docker-compose.yaml" or "Dockerfile")) detected.Add("Docker");
            if (extensions.ContainsKey(".mq5") || extensions.ContainsKey(".mq4") || extensions.ContainsKey(".mqh")) detected.Add("MQL5 (MetaTrader)");
            return detected.Any() ? string.Join(", ", detected) : "알 수 없음";
        }
    }
}