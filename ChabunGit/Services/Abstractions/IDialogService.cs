// File: ChabunGit/Services/Abstractions/IDialogService.cs
namespace ChabunGit.Services.Abstractions
{
    public interface IDialogService
    {
        string? ShowFolderBrowserDialog(string description);
        void ShowMessage(string message, string caption);
        bool ShowConfirmation(string message, string caption);
        void ShowPrompt(string title, string promptText);
        string? ShowGitignoreEditor(string initialContent);
        void ShowCommitDetails(string commitHash, string commitDetails);

    }
}