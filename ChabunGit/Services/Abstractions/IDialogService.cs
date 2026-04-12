// File: ChabunGit/Services/Abstractions/IDialogService.cs
using System;
using System.Collections.Generic;

namespace ChabunGit.Services.Abstractions
{
    public interface IDialogService
    {
        string? ShowFolderBrowserDialog(string description);
        void ShowMessage(string message, string caption);
        bool ShowConfirmation(string message, string caption);
        void ShowPrompt(string title, string promptText, bool isForCommitAi = false);
        string? ShowGitignoreEditor(string initialContent);
        void ShowCommitDetails(string commitHash, string commitDetails);
        void ShowAiCommitResult(string aiResultText, Action<string, string> onApply);
        void ShowAiGitignoreResult(string aiResultText, Action<string> onApply);

        // ▼▼▼ [추가] .gitignore 생성 전 제외 폴더 선택 다이얼로그 ▼▼▼
        List<string>? ShowFolderSelector(string repoPath);
        // ▲▲▲ [추가] 여기까지 ▲▲▲
    }
}
