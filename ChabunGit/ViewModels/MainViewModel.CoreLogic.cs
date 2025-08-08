// File: ChabunGit/ViewModels/MainViewModel.CoreLogic.cs
using ChabunGit.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
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

            IsLocalRepoWithoutRemote = !await _gitService.HasRemoteAsync(SelectedFolder!);
            if (IsLocalRepoWithoutRemote)
            {
                AddLog("⚠️ 원격 저장소가 연결되지 않았습니다. 연결을 진행해주세요.");
            }

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
    }
}