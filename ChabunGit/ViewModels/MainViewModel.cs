// File: ChabunGit/ViewModels/MainViewModel.cs
using ChabunGit.Core; // [수정] GitCommandExecutor를 사용하기 위해 네임스페이스를 추가합니다.
using ChabunGit.Models;
using ChabunGit.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IGitService _gitService;
        private readonly IDialogService _dialogService;
        private readonly IConfigManager _configManager;
        private readonly IPromptService _promptService;
        
        // ▼▼▼ [삭제] _currentDiff 필드는 더 이상 필요하지 않습니다. ▼▼▼
        // private string _currentDiff = string.Empty;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isNewProjectGuideActive;
        [ObservableProperty] private string? _selectedFolder;
        [ObservableProperty] private string _currentBranch = "현재 브랜치: N/A";
        [ObservableProperty] private string _fetchStatus = "초기화";
        [ObservableProperty][NotifyCanExecuteChangedFor(nameof(CommitCommand))] private string _commitTitle = "";
        [ObservableProperty] private string _commitBody = "";
        [ObservableProperty] private string _titleCharCountText = "제목 (50자 제한) : 0/50";
        [ObservableProperty] private bool _isForcePushChecked;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyLogCommand))]
        private string _logText = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ResetToCommitCommand))]
        private CommitInfo? _selectedCommit;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddRemoteCommand))]
        private string _newProjectGitHubUrl = "";

        [ObservableProperty]
        // ▼▼▼ [수정] AnalyzeChangesCommand의 CanExecute는 IsRepoValid에만 의존하도록 변경합니다. ▼▼▼
        [NotifyCanExecuteChangedFor(nameof(FetchCommand), nameof(PushCommand), nameof(CommitCommand), nameof(UndoLastCommitCommand), nameof(ResetToCommitCommand), nameof(EditGitignoreCommand), nameof(GenerateGitignorePromptCommand), nameof(AnalyzeChangesCommand))]
        private bool _isRepoValid;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PullCommand))]
        private bool _canPull;

        // ▼▼▼ [삭제] CanCreateFinalPrompt와 관련된 속성은 이제 필요 없습니다. ▼▼▼
        // [ObservableProperty]
        // [NotifyCanExecuteChangedFor(nameof(CreateFinalPromptCommand))]
        // private bool _canCreateFinalPrompt;

        public ObservableCollection<string> ChangedFiles { get; } = new();
        public ObservableCollection<CommitInfo> CommitHistory { get; } = new();

        public bool CanUndoLastCommit => IsRepoValid && CommitHistory.Any();

        [ObservableProperty] private bool _guideCanInit = true;
        [ObservableProperty] private bool _guideCanAddRemote;
        [ObservableProperty] private bool _guideCanComplete;

        public MainViewModel(IGitService gitService, IDialogService dialogService, IConfigManager configManager, IPromptService promptService, GitCommandExecutor gitCommandExecutor)
        {
            _gitService = gitService;
            _dialogService = dialogService;
            _configManager = configManager;
            _promptService = promptService;
            
            gitCommandExecutor.OnCommandExecuting += command => AddLog($"▶️ {command}");

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
                await _gitService.EnsureUtf8ConfigAsync(folderPath);

                IsNewProjectGuideActive = false;
                await RefreshRepositoryInfoAsync();
            }
            else
            {
                IsNewProjectGuideActive = true;
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

            CanPull = false;
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
            CanPull = false;
            AddLog("원격 저장소 확인 중 (Fetch)...");

            var fetchResult = await _gitService.FetchAsync(SelectedFolder!);
            AddLog(fetchResult.Output + fetchResult.Error);

            var statusResult = await _gitService.Executor.ExecuteAsync(SelectedFolder!, "status -sb");
            string status = statusResult.Output.Trim();

            if (status.Contains("behind"))
            {
                FetchStatus = "⚠️ 경고: 팀원이 올린 새로운 내용이 있습니다. Pull 하세요.";
                CanPull = true;
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
            // ▼▼▼ [수정] isForCommitAi 플래그를 사용하지 않음 (기본값 false) ▼▼▼
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
                await _gitService.EnsureUtf8ConfigAsync(SelectedFolder);

                AddLog("✅ Git 저장소 초기화 성공!");
                IsRepoValid = true;
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
                NewProjectGitHubUrl = "";
                GuideCanAddRemote = false;
                GuideCanComplete = true;
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

            IsNewProjectGuideActive = false;

            await RefreshRepositoryInfoAsync();
            IsBusy = false;
        }

        private bool CanCopyLog() => !string.IsNullOrWhiteSpace(LogText);

        [RelayCommand(CanExecute = nameof(CanCopyLog))]
        private void CopyLog()
        {
            Clipboard.SetText(LogText);
            AddLog("✅ 로그 내용이 클립보드에 복사되었습니다.");
        }

        // 1단계 커맨드: 변경점 분석
        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task AnalyzeChangesAsync()
        {
            IsBusy = true;
            // ▼▼▼ [삭제] CanCreateFinalPrompt = false; 줄을 삭제합니다. ▼▼▼
            AddLog("변경점 분석을 시작합니다...");

            // ▼▼▼ [수정] Diff를 가져오는 로직을 수정합니다. ▼▼▼
            string diffContent = await _promptService.GetDiffAsync(SelectedFolder!);
            
            // ▼▼▼ [수정] ShowPrompt 메서드를 수정된 시그니처에 맞게 호출하고, diffContent를 전달합니다. ▼▼▼
            // AI 커밋 프롬프트 생성을 위한 특별한 경우임을 알리는 플래그를 true로 설정합니다.
            _dialogService.ShowPrompt("AI 커밋 메시지 생성", diffContent, isForCommitAi: true);
            
            AddLog("변경점 분석 완료. AI 커밋 메시지 생성 창이 열렸습니다.");

            IsBusy = false;
        }

        // 2단계 커맨드: 최종 AI 프롬프트 생성
        // ▼▼▼ [삭제] CreateFinalPromptAsync 커맨드를 삭제합니다. ▼▼▼
        
        [RelayCommand]
        private async Task ShowCommitDetailsAsync(CommitInfo? commit)
        {
            if (commit == null || string.IsNullOrEmpty(SelectedFolder)) return;

            IsBusy = true;
            AddLog($"커밋 상세 정보 조회 중: {commit.ShortHash}");

            var result = await _gitService.GetCommitDetailsAsync(SelectedFolder, commit.Hash);

            if (result.ExitCode == 0)
            {
                _dialogService.ShowCommitDetails(commit.ShortHash, result.Output);
                AddLog("커밋 상세 정보 표시 완료.");
            }
            else
            {
                _dialogService.ShowMessage($"커밋 정보를 가져오는 중 오류가 발생했습니다:\n{result.Error}", "오류");
                AddLog($"커밋 정보 조회 실패: {result.Error}");
            }

            IsBusy = false;
        }
    }
}