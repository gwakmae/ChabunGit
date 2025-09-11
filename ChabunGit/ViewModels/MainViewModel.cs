// File: ChabunGit/ViewModels/MainViewModel.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System.IO;
using System.Threading.Tasks;
using System;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IGitService _gitService;
        private readonly IDialogService _dialogService;
        private readonly IConfigManager _configManager;
        private readonly IPromptService _promptService;

        public MainViewModel(IGitService gitService, IDialogService dialogService, IConfigManager configManager, IPromptService promptService, GitCommandExecutor gitCommandExecutor)
        {
            _gitService = gitService;
            _dialogService = dialogService;
            _configManager = configManager;
            _promptService = promptService;

            // 의존성 주입받은 서비스들을 각 partial 클래스에서 사용할 수 있도록 필드에 저장합니다.
            gitCommandExecutor.OnCommandExecuting += command => AddLog($"▶️ {command}");

            // CommitHistory 컬렉션이 변경될 때마다 CanUndoLastCommit 속성의 CanExecute 상태를 갱신하도록 설정합니다.
            CommitHistory.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CanUndoLastCommit));
        }

        public async Task InitializeAsync()
        {
            // 애플리케이션 시작 시 마지막으로 열었던 폴더를 불러옵니다.
            var settings = await _configManager.LoadConfigAsync();
            if (!string.IsNullOrEmpty(settings.LastOpenedFolderPath) && Directory.Exists(settings.LastOpenedFolderPath))
            {
                await SelectProjectFolder(settings.LastOpenedFolderPath);
            }
        }

        // ▼▼▼ [추가] index.lock 감지 시 사용자 확인 후 잠금 해제 및 재시도를 수행하는 헬퍼 메서드 ▼▼▼
        private async Task<bool> TryStageWithLockFixAsync()
        {
            var stageResult = await _gitService.StageAllChangesAsync(SelectedFolder!);
            AddLog(stageResult.Output + stageResult.Error);

            if (stageResult.ExitCode == 0) return true;

            // 오류 메시지에 'index.lock'이 포함되어 있는지 확인
            if (stageResult.Error.Contains("index.lock", StringComparison.OrdinalIgnoreCase))
            {
                var confirmed = _dialogService.ShowConfirmation(
                    "Git 잠금 파일(index.lock)이 감지되었습니다. 다른 Git 프로그램이 실행 중이거나 이전 작업이 비정상 종료된 것 같습니다.\n\n" +
                    "잠금 파일을 제거하고 다시 시도할까요?\n(주의: 다른 프로그램에서 중요한 작업을 하고 있다면 먼저 해당 작업을 완료하세요.)",
                    "Git 잠금 감지"
                );

                if (!confirmed) return false;

                bool unlocked = await _gitService.TryUnlockIndexAsync(SelectedFolder!);
                if (unlocked)
                {
                    AddLog("🔓 index.lock 파일을 제거했습니다. 스테이징을 다시 시도합니다.");
                    var retryResult = await _gitService.StageAllChangesAsync(SelectedFolder!);
                    AddLog(retryResult.Output + retryResult.Error);
                    return retryResult.ExitCode == 0;
                }
                else
                {
                    _dialogService.ShowMessage("index.lock 파일을 삭제하지 못했습니다. 파일 권한을 확인하거나, 파일을 사용 중인 다른 프로그램을 종료하고 다시 시도해주세요.", "삭제 실패");
                    return false;
                }
            }

            // index.lock 문제가 아닌 다른 오류
            return false;
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲
    }
}
