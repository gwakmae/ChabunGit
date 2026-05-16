// File: ChabunGit/Services/Abstractions/IPromptService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IPromptService
    {
        // ▼▼▼ [추가] 로그 콜백 ▼▼▼
        Action<string>? OnLog { get; set; }
        // ▲▲▲ [추가] 여기까지 ▲▲▲

        Task<string> CreateInitialCommitPromptAsync(string repoPath);

        Task<string> CreateGitignorePromptAsync(string repoPath,
            List<string>? excludedPaths = null);

        Task<string> GenerateGitignoreContentAsync(string repoPath,
            List<string>? excludedPaths = null);

        Task<string> GetDiffAsync(string repoPath);
        string CreateCommitPrompt(string diffContent);

        Task<string> GenerateCommitMessageAsync(string repoPath, IProgress<string>? progress = null);
        Task<string> GenerateInitialCommitMessageAsync(string repoPath, IProgress<string>? progress = null);
    }
}
