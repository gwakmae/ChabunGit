// File: ChabunGit/Services/Abstractions/IPromptService.cs
using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IPromptService
    {
        Task<string> CreateInitialCommitPromptAsync(string repoPath);
        Task<string> CreateCommitMessagePromptAsync(string repoPath);
        Task<string> CreateGitignorePromptAsync(string repoPath);
    }
}