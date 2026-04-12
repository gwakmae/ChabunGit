// File: ChabunGit/Services/Abstractions/IPromptService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IPromptService
    {
        Task<string> CreateInitialCommitPromptAsync(string repoPath);

        // ▼▼▼ [수정] excludedPaths 파라미터 추가 ▼▼▼
        Task<string> CreateGitignorePromptAsync(string repoPath,
            List<string>? excludedPaths = null);

        Task<string> GenerateGitignoreContentAsync(string repoPath,
            List<string>? excludedPaths = null);
        // ▲▲▲ [수정] 여기까지 ▲▲▲

        Task<string> GetDiffAsync(string repoPath);
        string CreateCommitPrompt(string diffContent);
        Task<string> GenerateCommitMessageAsync(string repoPath);
        Task<string> GenerateInitialCommitMessageAsync(string repoPath);
    }
}