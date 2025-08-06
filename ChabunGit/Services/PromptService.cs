// File: ChabunGit/Services/PromptService.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly GitCommandExecutor _executor;
        public PromptService(GitCommandExecutor executor) { _executor = executor; }

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
                var ignoreDirs = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", ".git", "node_modules" };
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
            catch (System.Exception ex)
            {
                promptBuilder.AppendLine($"\n파일을 읽는 중 오류 발생: {ex.Message}");
            }

            return promptBuilder.ToString();
        }

        public async Task<string> CreateCommitMessagePromptAsync(string repoPath)
        {
            var diffResult = await _executor.ExecuteAsync(repoPath, "diff HEAD");

            if (diffResult.ExitCode != 0 || string.IsNullOrWhiteSpace(diffResult.Output))
            {
                return "커밋할 변경 사항이 없습니다. 먼저 코드를 수정하거나, 스테이징되지 않은 변경 사항이 있는지 확인해주세요.";
            }

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("아래는 'git diff' 결과입니다. 이 변경 사항에 대한 커밋 메시지를 다음 규칙에 따라 영어로 작성해 주세요.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("1.  **제목 (Subject):**");
            promptBuilder.AppendLine("    *   변경 사항을 요약하는 한 줄의 제목을 작성합니다.");
            promptBuilder.AppendLine("    *   **반드시 50자 이내**로 작성해야 합니다.");
            promptBuilder.AppendLine("    *   명령형으로 작성합니다 (예: 'Fix' not 'Fixed', 'Add' not 'Added').");
            promptBuilder.AppendLine("    *   제목 끝에 마침표를 찍지 마세요.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("2.  **본문 (Body):**");
            promptBuilder.AppendLine("    *   제목 아래에 한 줄을 비웁니다.");
            promptBuilder.AppendLine("    *   무엇을, 왜 변경했는지 자세히 설명합니다.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("--- diff 내용 ---");
            promptBuilder.AppendLine(diffResult.Output);

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
            catch (System.Exception ex)
            {
                return Task.FromResult($"프로젝트 파일 분석 중 오류가 발생했습니다: {ex.Message}\n\nC# WPF 프로젝트를 위한 표준 .gitignore 파일을 생성해 주세요.");
            }

            return Task.FromResult(promptBuilder.ToString());
        }
    }
}