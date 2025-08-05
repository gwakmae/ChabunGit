// 파일 경로: ChabunGit/Services/GitService.cs (수정된 코드)

using ChabunGit.Core;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static ChabunGit.Core.GitCommandExecutor;

namespace ChabunGit.Services
{
    /// <summary>
    /// 순수 Git 명령어 실행과 관련된 로직을 총괄하는 서비스 클래스입니다.
    /// </summary>
    public class GitService
    {
        public readonly GitCommandExecutor Executor;

        public GitService(GitCommandExecutor executor)
        {
            Executor = executor;
        }

        public bool IsGitRepository(string path) => Directory.Exists(Path.Combine(path, ".git"));
        public async Task<ProcessResult> InitRepositoryAsync(string path) => await Executor.ExecuteAsync(path, "init");
        public async Task<ProcessResult> AddRemoteAsync(string repoPath, string remoteUrl) => await Executor.ExecuteAsync(repoPath, $"remote add origin {remoteUrl}");
        public async Task<ProcessResult> SetMainBranchAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "branch -M main");
        public async Task<ProcessResult> GetChangedFilesAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "status --porcelain");
        public async Task<ProcessResult> GetCommitHistoryAsync(string repoPath, int maxCount = 50)
        {
            string format = "--pretty=format:\"%h|%ad|%s\"";
            string dateformat = "--date=short";
            return await Executor.ExecuteAsync(repoPath, $"log -{maxCount} {format} {dateformat}");
        }
        public async Task<ProcessResult> StageAllChangesAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "add .");
        public async Task<ProcessResult> CommitAsync(string repoPath, string title, string body)
        {
            var commandBuilder = new StringBuilder();
            commandBuilder.Append($"commit -m \"{title.Trim()}\"");
            var bodyLines = body.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in bodyLines)
            {
                commandBuilder.Append($" -m \"{line.Trim()}\"");
            }
            return await Executor.ExecuteAsync(repoPath, commandBuilder.ToString());
        }
        public async Task<ProcessResult> FetchAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "fetch");
        public async Task<ProcessResult> PullAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "pull");
        public async Task<ProcessResult> PushAsync(string repoPath, bool isForce, bool isFirstPush)
        {
            string command = "push";
            if (isFirstPush) command += " -u origin HEAD";
            if (isForce) command += " --force";
            return await Executor.ExecuteAsync(repoPath, command);
        }
        public async Task<ProcessResult> ResetLastCommitAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "reset --soft HEAD~1");
        public async Task<ProcessResult> ResetToCommitAsync(string repoPath, string commitHash) => await Executor.ExecuteAsync(repoPath, $"reset --hard {commitHash}");
    }
}