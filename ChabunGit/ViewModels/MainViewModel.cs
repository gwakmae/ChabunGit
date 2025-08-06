// File: ChabunGit/ViewModels/MainViewModel.cs
using ChabunGit.Models;
using ChabunGit.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // Clipboard 클래스를 사용하기 위해 추가

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IGitService _gitService;
        private readonly IDialogService _dialogService;
        private readonly IConfigManager _configManager;
        private readonly IPromptService _promptService;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isNewProjectGuideActive;
        [ObservableProperty] private string? _selectedFolder;
        [ObservableProperty] private string _currentBranch = "현재 브랜치: N/A";
        [ObservableProperty] private string _fetchStatus = "초기화";
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CommitCommand))] private string _commitTitle = "";
        [ObservableProperty] private string _commitBody = "";
        [ObservableProperty] private string _titleCharCountText = "제목 (50자 제한) : 0/50";
        [ObservableProperty] private bool _isForcePushChecked;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyLogCommand))]
        private string _logText = "";
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ResetToCommitCommand))] private CommitInfo? _selectedCommit;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddRemoteCommand))]
        private string _newProjectGitHubUrl = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FetchCommand))]
        [NotifyCanExecuteChangedFor(nameof(PullCommand))]
        [NotifyCanExecuteChangedFor(nameof(PushCommand))]
        [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
        [NotifyCanExecuteChangedFor(nameof(GenerateCommitPromptCommand))]
        [NotifyCanExecuteChangedFor(nameof(UndoLastCommitCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetToCommitCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditGitignoreCommand))]
        [NotifyCanExecuteChangedFor(nameof(GenerateGitignorePromptCommand))]
        private bool _isRepoValid;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PullCommand))]
        private bool _canPull;

        public ObservableCollection<string> ChangedFiles { get; } = new();
        public ObservableCollection<CommitInfo> CommitHistory { get; } = new();
        
        // UndoLastCommitCommand를 위한 CanExecute 프로퍼티
        public bool CanUndoLastCommit => IsRepoValid && CommitHistory.Any();

        // '새 프로젝트 가이드'의 단계별 활성화 상태를 제어하는 프로퍼티
        [ObservableProperty] private bool _guideCanInit = true;
        [ObservableProperty] private bool _guideCanAddRemote;
        [ObservableProperty] private bool _guideCanComplete;

        public MainViewModel(IGitService gitService, IDialogService dialogService, IConfigManager configManager, IPromptService promptService)
        {
            _gitService = gitService;
            _dialogService = dialogService;
            _configManager = configManager;
            _promptService = promptService;
            
            // 컬렉션이 변경될 때마다 CanExecute 상태를 갱신하도록 설정
            CommitHistory.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CanUndoLastCommit));
        }

        public async Task InitializeAsync()
        {
            var settings = await _configManager.LoadConfigAsync();
            if (!string.IsNullOrEmpty(settings.LastOpenedFolderPath) && Directory.Exists(settings.LastOpenedFolderPath))
            {
                await SelectProjectFolder(settings.LastOpenedFolderPath);
            }
        }

        private async Task SelectProjectFolder(string folderPath)
        {
            SelectedFolder = folderPath;
            await _configManager.SaveConfigAsync(new AppSettings { LastOpenedFolderPath = folderPath });

            if (_gitService.IsGitRepository(folderPath))
            {
                IsNewProjectGuideActive = false;
                await RefreshRepositoryInfoAsync();
            }
            else
            {
                IsNewProjectGuideActive = true;
                // 가이드 UI 상태 초기화
                GuideCanInit = true;
                GuideCanAddRemote = false;
                GuideCanComplete = false;
                NewProjectGitHubUrl = "";
            }
        }

        partial void OnSelectedFolderChanged(string? value)
        {
            IsRepoValid = !string.IsNullOrEmpty(value) && _gitService.IsGitRepository(value);
        }

        private async Task RefreshRepositoryInfoAsync()
        {
            if (!IsRepoValid) return;

            IsBusy = true;
            AddLog("저장소 정보를 갱신합니다...");

            var branchResult = await _gitService.Executor.ExecuteAsync(SelectedFolder!, "rev-parse --abbrev-ref HEAD");
            CurrentBranch = branchResult.ExitCode == 0 ? $"현재 브랜치: {branchResult.Output.Trim()}" : "현재 브랜치: N/A";

            var statusResult = await _gitService.GetChangedFilesAsync(SelectedFolder!);
            ChangedFiles.Clear();
            statusResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(line => ChangedFiles.Add(line.Trim()));

            var historyResult = await _gitService.GetCommitHistoryAsync(SelectedFolder!);
            CommitHistory.Clear();
            historyResult.ForEach(c => CommitHistory.Add(c));

            CanPull = false; // 정보 갱신 시 Pull 가능 상태 초기화
            FetchStatus = "원격 저장소 상태를 확인하려면 [Fetch] 버튼을 누르세요.";
            IsBusy = false;
            AddLog("정보 갱신 완료.");
        }

        private void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            LogText += $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}{Environment.NewLine}";
        }

        partial void OnCommitTitleChanged(string value)
        {
            TitleCharCountText = $"제목 (50자 제한) : {value.Length}/50";
        }

        [RelayCommand]
        private async Task SelectFolderAsync()
        {
            var folderPath = _dialogService.ShowFolderBrowserDialog("프로젝트 폴더를 선택하세요.");
            if (!string.IsNullOrEmpty(folderPath))
            {
                await SelectProjectFolder(folderPath);
            }
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task FetchAsync()
        {
            IsBusy = true;
            CanPull = false; // Fetch 시작 시 우선 비활성화
            AddLog("원격 저장소 확인 중 (Fetch)...");

            var fetchResult = await _gitService.FetchAsync(SelectedFolder!);
            AddLog(fetchResult.Output + fetchResult.Error);

            var statusResult = await _gitService.Executor.ExecuteAsync(SelectedFolder!, "status -sb");
            string status = statusResult.Output.Trim();

            if (status.Contains("behind"))
            {
                FetchStatus = "⚠️ 경고: 팀원이 올린 새로운 내용이 있습니다. Pull 하세요.";
                CanPull = true; // Pull이 필요한 상태이므로 활성화
            }
            else if (status.Contains("ahead")) FetchStatus = "✅ 원격 저장소보다 앞서 있습니다. Push 하세요.";
            else if (status.Contains("up-to-date") || !status.Contains("origin")) FetchStatus = "✅ 원격 저장소와 동기화됨.";
            else FetchStatus = "원격 저장소 상태를 확인할 수 없습니다.";

            IsBusy = false;
        }

        private bool CanPullExecute() => IsRepoValid && CanPull;

        [RelayCommand(CanExecute = nameof(CanPullExecute))]
        private async Task PullAsync()
        {
            IsBusy = true;
            AddLog("원격 내용 가져오는 중 (Pull)...");

            var pullResult = await _gitService.PullAsync(SelectedFolder!);
            AddLog(pullResult.Output + pullResult.Error);

            if (pullResult.ExitCode == 0)
            {
                AddLog("✅ Pull 성공!");
                await RefreshRepositoryInfoAsync();
            }
            else
            {
                _dialogService.ShowMessage($"Pull 중 오류가 발생했습니다.\n로그를 확인하고 충돌을 수동으로 해결해주세요.\n\n{pullResult.Error}", "Pull 오류");
            }
            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task PushAsync()
        {
            if (IsForcePushChecked && !_dialogService.ShowConfirmation("경고: 강제 푸시는 원격 저장소의 이력을 덮어씁니다. 정말 진행하시겠습니까?", "강제 푸시 경고"))
            {
                return;
            }

            IsBusy = true;
            AddLog("원격 저장소에 공유 중 (Push)...");

            var remoteResult = await _gitService.Executor.ExecuteAsync(SelectedFolder!, "branch -vv");
            bool isFirstPush = !remoteResult.Output.Contains("[origin/");

            var pushResult = await _gitService.PushAsync(SelectedFolder!, IsForcePushChecked, isFirstPush);
            AddLog(pushResult.Output + pushResult.Error);

            if (pushResult.ExitCode == 0)
            {
                AddLog("✅ Push 성공!");
                await RefreshRepositoryInfoAsync();
            }
            else
            {
                _dialogService.ShowMessage($"Push 중 오류가 발생했습니다: {pushResult.Error}", "오류");
            }

            IsBusy = false;
        }

        private bool CanCommit() => IsRepoValid && !string.IsNullOrWhiteSpace(CommitTitle);

        [RelayCommand(CanExecute = nameof(CanCommit))]
        private async Task CommitAsync()
        {
            IsBusy = true;
            AddLog("변경 사항 스테이징 중...");
            var stageResult = await _gitService.StageAllChangesAsync(SelectedFolder!);
            AddLog(stageResult.Output + stageResult.Error);
            if (stageResult.ExitCode != 0) { IsBusy = false; return; }

            AddLog("커밋 생성 중...");
            var commitResult = await _gitService.CommitAsync(SelectedFolder!, CommitTitle, CommitBody);
            AddLog(commitResult.Output + commitResult.Error);

            if (commitResult.ExitCode == 0)
            {
                AddLog("✅ 커밋 성공!");
                CommitTitle = "";
                CommitBody = "";
                await RefreshRepositoryInfoAsync();
            }
            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task GenerateCommitPromptAsync()
        {
            IsBusy = true;
            AddLog("AI 질문지를 만드는 중...");

            string prompt = CommitHistory.Any()
                ? await _promptService.CreateCommitMessagePromptAsync(SelectedFolder!)
                : await _promptService.CreateInitialCommitPromptAsync(SelectedFolder!);

            string formTitle = CommitHistory.Any() ? "변경 사항 커밋 메시지 질문지 생성" : "첫 커밋 메시지 질문지 생성";

            IsBusy = false;
            AddLog("AI 질문지 생성 완료.");
            _dialogService.ShowPrompt(formTitle, prompt);
        }

        [RelayCommand(CanExecute = nameof(CanUndoLastCommit))]
        private async Task UndoLastCommitAsync()
        {
            if (!_dialogService.ShowConfirmation("마지막 커밋을 취소하시겠습니까? (이 커밋으로 변경된 내용은 남아있습니다)", "커밋 취소")) return;

            IsBusy = true;
            AddLog("마지막 커밋 취소 중 (soft reset)...");
            var result = await _gitService.ResetLastCommitAsync(SelectedFolder!);
            AddLog(result.Output + result.Error);
            await RefreshRepositoryInfoAsync();
            IsBusy = false;
        }

        private bool CanResetToCommit() => IsRepoValid && SelectedCommit != null;
        [RelayCommand(CanExecute = nameof(CanResetToCommit))]
        private async Task ResetToCommitAsync()
        {
            string selectedHash = SelectedCommit!.ShortHash;
            if (!_dialogService.ShowConfirmation($"경고: {selectedHash} 시점으로 돌아가면 이후의 모든 커밋과 현재 변경 사항이 사라집니다. 정말 진행하시겠습니까?", "과거로 돌아가기")) return;

            IsBusy = true;
            AddLog($"{selectedHash} 시점으로 돌아가는 중 (hard reset)...");
            var result = await _gitService.ResetToCommitAsync(SelectedFolder!, SelectedCommit.Hash);
            AddLog(result.Output + result.Error);
            await RefreshRepositoryInfoAsync();
            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task EditGitignoreAsync()
        {
            string gitignorePath = Path.Combine(SelectedFolder!, ".gitignore");
            string content = File.Exists(gitignorePath) ? await File.ReadAllTextAsync(gitignorePath) : "# .NET 프로젝트 무시 파일 예시\n[Bb]in/\n[Oo]bj/";

            string? newContent = _dialogService.ShowGitignoreEditor(content);
            if (newContent != null)
            {
                await File.WriteAllTextAsync(gitignorePath, newContent);
                AddLog(".gitignore 파일이 수정되었습니다. 변경 파일 목록을 갱신합니다.");
                await RefreshRepositoryInfoAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task GenerateGitignorePromptAsync()
        {
            IsBusy = true;
            AddLog(".gitignore 생성을 위한 AI 질문지를 만드는 중...");
            string prompt = await _promptService.CreateGitignorePromptAsync(SelectedFolder!);
            IsBusy = false;
            AddLog("AI 질문지 생성 완료.");
            _dialogService.ShowPrompt(".gitignore 질문지 생성", prompt);
        }

        [RelayCommand(CanExecute = nameof(GuideCanInit))]
        private async Task InitializeGitAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            AddLog("Git 저장소 초기화 중...");
            var result = await _gitService.InitRepositoryAsync(SelectedFolder);
            AddLog(result.Output + result.Error);
            
            if (result.ExitCode == 0)
            {
                AddLog("✅ Git 저장소 초기화 성공!");
                IsRepoValid = true; // 이제 유효한 저장소
                GuideCanInit = false;
                GuideCanAddRemote = true;
            }
            IsBusy = false;
        }

        private bool CanAddRemote() => GuideCanAddRemote && !string.IsNullOrWhiteSpace(NewProjectGitHubUrl);

        [RelayCommand(CanExecute = nameof(CanAddRemote))]
        private async Task AddRemoteAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            AddLog("원격 저장소 연결 중...");
            var result = await _gitService.AddRemoteAsync(SelectedFolder, NewProjectGitHubUrl.Trim());
            AddLog(result.Output + result.Error);

            if (result.ExitCode == 0)
            {
                _dialogService.ShowMessage("원격 저장소가 성공적으로 연결되었습니다.", "성공");
                NewProjectGitHubUrl = ""; // 입력 필드 초기화
                GuideCanAddRemote = false; // 원격 추가 비활성화
                GuideCanComplete = true;   // 완료 버튼 활성화
            }
            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(GuideCanComplete))]
        private async Task CompleteGuideAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            AddLog("주 브랜치를 'main'으로 설정 중...");
            await _gitService.SetMainBranchAsync(SelectedFolder);
            
            IsNewProjectGuideActive = false; // 가이드 종료
            
            await RefreshRepositoryInfoAsync();
            IsBusy = false;
        }

        // 로그 복사 커맨드의 활성화 조건을 정의하는 메서드
        private bool CanCopyLog() => !string.IsNullOrWhiteSpace(LogText);

        [RelayCommand(CanExecute = nameof(CanCopyLog))]
        private void CopyLog()
        {
            Clipboard.SetText(LogText);
            AddLog("✅ 로그 내용이 클립보드에 복사되었습니다.");
        }
    }
}