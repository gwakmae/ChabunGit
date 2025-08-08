// File: ChabunGit/ViewModels/MainViewModel.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System.IO;
using System.Threading.Tasks;

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
    }
}