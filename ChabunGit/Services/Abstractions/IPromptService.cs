using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IPromptService
    {
        Task<string> CreateInitialCommitPromptAsync(string repoPath);
        Task<string> CreateGitignorePromptAsync(string repoPath);

         Task<string> GetDiffAsync(string repoPath);
        string CreateCommitPrompt(string diffContent);
    }
}