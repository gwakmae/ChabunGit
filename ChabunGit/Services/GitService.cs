// File: ChabunGit/Services/GitService.cs
using ChabunGit.Core;
using ChabunGit.Models;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class GitService : IGitService
    {
        public GitCommandExecutor Executor { get; }

        public GitService(GitCommandExecutor executor)
        {
            Executor = executor;
        }

        public bool IsGitRepository(string path) => Directory.Exists(Path.Combine(path, ".git"));
        public async Task<GitCommandExecutor.ProcessResult> InitRepositoryAsync(string path) => await Executor.ExecuteAsync(path, "init");
        public async Task<GitCommandExecutor.ProcessResult> AddRemoteAsync(string repoPath, string remoteUrl) => await Executor.ExecuteAsync(repoPath, $"remote add origin {remoteUrl}");
        public async Task<GitCommandExecutor.ProcessResult> SetMainBranchAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "branch -M main");
        public async Task<GitCommandExecutor.ProcessResult> GetChangedFilesAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "status --porcelain");

        public async Task<List<CommitInfo>> GetCommitHistoryAsync(string repoPath, int maxCount = 50)
        {
            string format = "--pretty=format:\"%H|%h|%ad|%s\"";
            string dateformat = "--date=short";
            var result = await Executor.ExecuteAsync(repoPath, $"log -{maxCount} {format} {dateformat}");

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output)) return new List<CommitInfo>();

            return result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('|'))
                .Where(parts => parts.Length == 4)
                .Select(parts => new CommitInfo
                {
                    Hash = parts[0].Trim(),
                    ShortHash = parts[1].Trim(),
                    Date = parts[2].Trim(),
                    Message = parts[3].Trim()
                })
                .ToList();
        }

        public async Task<GitCommandExecutor.ProcessResult> StageAllChangesAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "add .");

        public async Task<GitCommandExecutor.ProcessResult> CommitAsync(string repoPath, string title, string body)
        {
            var commandBuilder = new StringBuilder();
            commandBuilder.Append($"commit -m \"{title.Trim()}\"");
            var bodyLines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in bodyLines)
            {
                commandBuilder.Append($" -m \"{line.Trim()}\"");
            }
            return await Executor.ExecuteAsync(repoPath, commandBuilder.ToString());
        }

        public async Task<GitCommandExecutor.ProcessResult> FetchAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "fetch");
        public async Task<GitCommandExecutor.ProcessResult> PullAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "pull");

        public async Task<GitCommandExecutor.ProcessResult> PushAsync(string repoPath, bool isForce, bool isFirstPush)
        {
            string command = "push";
            if (isFirstPush) command += " -u origin HEAD";
            if (isForce) command += " --force";
            return await Executor.ExecuteAsync(repoPath, command);
        }

        public async Task<GitCommandExecutor.ProcessResult> ResetLastCommitAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "reset --soft HEAD~1");
        public async Task<GitCommandExecutor.ProcessResult> ResetToCommitAsync(string repoPath, string commitHash) => await Executor.ExecuteAsync(repoPath, $"reset --hard {commitHash}");
    }
}