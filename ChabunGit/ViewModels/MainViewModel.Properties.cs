// File: ChabunGit/ViewModels/MainViewModel.Properties.cs
using ChabunGit.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
        // UI 상태 관련 속성
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isNewProjectGuideActive;
        [ObservableProperty] private bool _isLocalRepoWithoutRemote;
        [ObservableProperty] private string _fetchStatus = "초기화";
        [ObservableProperty] private bool _isForcePushChecked;

        [ObservableProperty]
        // ▼▼▼ [수정] RefreshCommand를 CanExecute 상태 변경 알림 목록 맨 앞에 추가합니다. ▼▼▼
        [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(FetchCommand), nameof(PushCommand), nameof(CommitCommand), nameof(UndoLastCommitCommand), nameof(ResetToCommitCommand), nameof(EditGitignoreCommand), nameof(GenerateGitignorePromptCommand), nameof(AnalyzeChangesCommand), nameof(StopTrackingFileCommand))]
        private bool _isRepoValid;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PullCommand))]
        private bool _canPull;

        // 데이터 관련 속성
        [ObservableProperty] private string? _selectedFolder;
        [ObservableProperty] private string _currentBranch = "현재 브랜치: N/A";
        [ObservableProperty][NotifyCanExecuteChangedFor(nameof(CommitCommand))] private string _commitTitle = "";
        [ObservableProperty] private string _commitBody = "";
        [ObservableProperty] private string _titleCharCountText = "제목 (50자 제한) : 0/50";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyLogCommand))]
        private string _logText = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ResetToCommitCommand))]
        private CommitInfo? _selectedCommit;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTrackingFileCommand))]
        private string? _selectedChangedFile;

        // 새 프로젝트 가이드 관련 속성
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddRemoteCommand))]
        private string _newProjectGitHubUrl = "";
        [ObservableProperty] private bool _guideCanInit = true;
        [ObservableProperty] private bool _guideCanAddRemote;
        [ObservableProperty] private bool _guideCanComplete;

        // 컬렉션
        public ObservableCollection<string> ChangedFiles { get; } = new();
        public ObservableCollection<CommitInfo> CommitHistory { get; } = new();

        // 계산 속성
        public bool CanUndoLastCommit => IsRepoValid && CommitHistory.Any();
    }
}