// File: ChabunGit/ViewModels/MainViewModel.HistoryCommands.cs
using ChabunGit.Models;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
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

        // ▼▼▼ [삭제] EditGitignoreAsync 커맨드를 MainViewModel.FileCommands.cs로 이동시켰으므로 이 파일에서는 삭제합니다. ▼▼▼
        // [RelayCommand(CanExecute = nameof(IsRepoValid))]
        // private async Task EditGitignoreAsync() { ... }
        // ▲▲▲ [삭제] 여기까지 ▲▲▲
    }
}