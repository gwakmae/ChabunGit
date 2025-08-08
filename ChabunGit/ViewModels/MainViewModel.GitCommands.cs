// File: ChabunGit/ViewModels/MainViewModel.GitCommands.cs
using ChabunGit.Models;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
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

            if (status.Contains("behind")) {
                FetchStatus = "⚠️ 경고: 팀원이 올린 새로운 내용이 있습니다. Pull 하세요.";
                CanPull = true;
            } else if (status.Contains("ahead")) FetchStatus = "✅ 원격 저장소보다 앞서 있습니다. Push 하세요.";
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
            if (pullResult.ExitCode == 0) {
                AddLog("✅ Pull 성공!");
                await RefreshRepositoryInfoAsync();
            } else {
                _dialogService.ShowMessage($"Pull 중 오류가 발생했습니다.\n로그를 확인하고 충돌을 수동으로 해결해주세요.\n\n{pullResult.Error}", "Pull 오류");
            }
            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task PushAsync()
        {
            if (IsForcePushChecked && !_dialogService.ShowConfirmation("경고: 강제 푸시는 원격 저장소의 이력을 덮어씁니다. 정말 진행하시겠습니까?", "강제 푸시 경고")) return;
            IsBusy = true;
            AddLog("원격 저장소에 공유 중 (Push)...");
            var remoteResult = await _gitService.Executor.ExecuteAsync(SelectedFolder!, "branch -vv");
            bool isFirstPush = !remoteResult.Output.Contains("[origin/");
            var pushResult = await _gitService.PushAsync(SelectedFolder!, IsForcePushChecked, isFirstPush);
            AddLog(pushResult.Output + pushResult.Error);

            if (pushResult.ExitCode == 0) {
                AddLog("✅ Push 성공!");
                await RefreshRepositoryInfoAsync();
            } else {
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

            if (commitResult.ExitCode == 0) {
                AddLog("✅ 커밋 성공!");
                CommitTitle = "";
                CommitBody = "";
                await RefreshRepositoryInfoAsync();
            }
            IsBusy = false;
        }
    }
}