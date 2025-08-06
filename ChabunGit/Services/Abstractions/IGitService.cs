// File: ChabunGit/Services/Abstractions/IGitService.cs
using ChabunGit.Core;
using ChabunGit.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IGitService
    {
        GitCommandExecutor Executor { get; }
        bool IsGitRepository(string path);
        Task<GitCommandExecutor.ProcessResult> InitRepositoryAsync(string path);
        Task<GitCommandExecutor.ProcessResult> AddRemoteAsync(string repoPath, string remoteUrl);
        Task<GitCommandExecutor.ProcessResult> SetMainBranchAsync(string repoPath);
        Task<GitCommandExecutor.ProcessResult> GetChangedFilesAsync(string repoPath);
        Task<List<CommitInfo>> GetCommitHistoryAsync(string repoPath, int maxCount = 50);
        Task<GitCommandExecutor.ProcessResult> StageAllChangesAsync(string repoPath);
        Task<GitCommandExecutor.ProcessResult> CommitAsync(string repoPath, string title, string body);
        Task<GitCommandExecutor.ProcessResult> FetchAsync(string repoPath);
        Task<GitCommandExecutor.ProcessResult> PullAsync(string repoPath);
        Task<GitCommandExecutor.ProcessResult> PushAsync(string repoPath, bool isForce, bool isFirstPush);
        Task<GitCommandExecutor.ProcessResult> ResetLastCommitAsync(string repoPath);
        Task<GitCommandExecutor.ProcessResult> ResetToCommitAsync(string repoPath, string commitHash);
    }
}