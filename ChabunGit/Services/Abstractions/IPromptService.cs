using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IPromptService
    {
        Task<string> CreateInitialCommitPromptAsync(string repoPath);
        Task<string> CreateGitignorePromptAsync(string repoPath);

        // 2단계 워크플로우를 위한 새로운 메서드 시그니처
        Task<string> GetTranslatedDiffAsync(string repoPath);
        string CreateFinalPromptFromDiffs(string originalDiff, string translatedDiff);
    }
}