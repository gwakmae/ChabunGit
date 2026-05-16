// File: ChabunGit/Services/Abstractions/IPromptService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IPromptService
    {
        Task<string> CreateInitialCommitPromptAsync(string repoPath);

        Task<string> CreateGitignorePromptAsync(string repoPath,
            List<string>? excludedPaths = null);

        Task<string> GenerateGitignoreContentAsync(string repoPath,
            List<string>? excludedPaths = null);

        Task<string> GetDiffAsync(string repoPath);
        string CreateCommitPrompt(string diffContent);

        // 진행률(Progress) 파라미터 추가
        Task<string> GenerateCommitMessageAsync(string repoPath, IProgress<string>? progress = null);
        Task<string> GenerateInitialCommitMessageAsync(string repoPath, IProgress<string>? progress = null);
    }
}