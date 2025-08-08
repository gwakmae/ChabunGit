// File: ChabunGit/ViewModels/MainViewModel.GuideAndAiCommands.cs
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
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
        private async Task GenerateGitignorePromptAsync()
        {
            IsBusy = true;
            AddLog(".gitignore 생성을 위한 AI 질문지를 만드는 중...");
            string prompt = await _promptService.CreateGitignorePromptAsync(SelectedFolder!);
            IsBusy = false;
            AddLog("AI 질문지 생성 완료.");
            _dialogService.ShowPrompt(".gitignore 질문지 생성", prompt);
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task AnalyzeChangesAsync()
        {
            IsBusy = true;
            AddLog("변경점 분석을 시작합니다...");
            string diffContent = await _promptService.GetDiffAsync(SelectedFolder!);
            _dialogService.ShowPrompt("AI 커밋 메시지 생성", diffContent, isForCommitAi: true);
            AddLog("변경점 분석 완료. AI 커밋 메시지 생성 창이 열렸습니다.");
            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(GuideCanInit))]
        private async Task InitializeGitAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            AddLog("Git 저장소 초기화 중...");
            var result = await _gitService.InitRepositoryAsync(SelectedFolder);
            AddLog(result.Output + result.Error);
            if (result.ExitCode == 0) {
                await _gitService.EnsureUtf8ConfigAsync(SelectedFolder);
                AddLog("✅ Git 저장소 초기화 성공!");
                IsRepoValid = true;
                GuideCanInit = false;
                GuideCanAddRemote = true;
            }
            IsBusy = false;
        }

        private bool CanAddRemote()
        {
            bool hasUrl = !string.IsNullOrWhiteSpace(NewProjectGitHubUrl);
            return hasUrl && (GuideCanAddRemote || IsLocalRepoWithoutRemote);
        }

        [RelayCommand(CanExecute = nameof(CanAddRemote))]
        private async Task AddRemoteAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            AddLog("원격 저장소 연결 중...");
            var result = await _gitService.AddRemoteAsync(SelectedFolder, NewProjectGitHubUrl.Trim());
            AddLog(result.Output + result.Error);
            if (result.ExitCode == 0) {
                _dialogService.ShowMessage("원격 저장소가 성공적으로 연결되었습니다.", "성공");
                NewProjectGitHubUrl = "";
                IsLocalRepoWithoutRemote = false;
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
    }
}