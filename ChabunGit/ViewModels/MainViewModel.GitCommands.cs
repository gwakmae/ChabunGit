// File: ChabunGit/ViewModels/MainViewModel.GitCommands.cs
using ChabunGit.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task RefreshAsync()
        {
            // 기존에 사용하던 저장소 정보 갱신 로직을 그대로 호출합니다.
            await RefreshRepositoryInfoAsync();
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

        // ▼▼▼ [수정] 안전 푸시 파이프라인으로 전면 교체 ▼▼▼
        // 흐름: Fetch → 동기화 상태 판별 → 필요 시 자동 rebase → Push
        //      → non-fast-forward 발생 시 자동 복구 1회 재시도
        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task PushAsync()
        {
            if (IsForcePushChecked && !_dialogService.ShowConfirmation(
                "경고: 강제 푸시는 원격 저장소의 이력을 덮어씁니다.\n" +
                "--force-with-lease가 적용되어, 다른 사람이 그 사이 푸시한 내용이 있으면 안전하게 거절됩니다.\n\n" +
                "정말 진행하시겠습니까?", "강제 푸시 경고")) return;

            IsBusy = true;
            try
            {
                // ── 1단계: 원격 상태 최신화 ─────────────────────────────
                AddLog("📡 원격 저장소 상태 확인 중 (Fetch)...");
                var fetchResult = await _gitService.FetchAsync(SelectedFolder!);
                if (fetchResult.ExitCode != 0)
                {
                    AddLog($"⚠️ Fetch 실패 (네트워크 또는 인증 문제일 수 있습니다):\n{fetchResult.Error}");
                }

                // ── 2단계: 로컬 vs 원격 상태 판별 ───────────────────────
                string syncStatus = await _gitService.GetRemoteSyncStatusAsync(SelectedFolder!);
                AddLog($"동기화 상태: {syncStatus}");

                // 뒤처져 있거나 갈라진 경우, 강제 푸시가 아니라면 rebase로 먼저 정리
                if (!IsForcePushChecked && (syncStatus == "behind" || syncStatus == "diverged"))
                {
                    AddLog("🔄 원격에 새 커밋이 있습니다. 로컬 변경을 rebase로 재배치합니다 (자동 stash 포함)...");

                    var rebaseResult = await _gitService.PullRebaseAsync(SelectedFolder!);
                    AddLog(rebaseResult.Output + rebaseResult.Error);

                    if (rebaseResult.ExitCode != 0)
                    {
                        if (rebaseResult.Error.Contains("conflict", StringComparison.OrdinalIgnoreCase)
                            || rebaseResult.Output.Contains("conflict", StringComparison.OrdinalIgnoreCase))
                        {
                            _dialogService.ShowMessage(
                                "rebase 중 충돌이 발생했습니다.\n\n" +
                                "충돌 파일을 수동으로 해결한 뒤:\n" +
                                "  git add <파일>\n" +
                                "  git rebase --continue\n\n" +
                                "중단하려면: git rebase --abort",
                                "Rebase 충돌");
                            return;
                        }

                        _dialogService.ShowMessage(
                            $"푸시 전 동기화(rebase)에 실패했습니다:\n\n{rebaseResult.Error}", "동기화 실패");
                        return;
                    }

                    AddLog("✅ Rebase 완료. 이제 안전하게 푸시할 수 있습니다.");
                }

                // ── 3단계: 푸시 실행 ────────────────────────────────────
                AddLog("원격 저장소에 공유 중 (Push)...");
                var remoteResult = await _gitService.Executor.ExecuteAsync(SelectedFolder!, "branch -vv");
                bool isFirstPush = !remoteResult.Output.Contains("[origin/");
                var pushResult = await _gitService.PushAsync(SelectedFolder!, IsForcePushChecked, isFirstPush);
                AddLog(pushResult.Output + pushResult.Error);

                // ── 4단계: non-fast-forward 발생 시 자동 복구 1회 시도 ──
                if (pushResult.ExitCode != 0 && IsNonFastForward(pushResult.Error) && !IsForcePushChecked)
                {
                    AddLog("⚠️ 푸시가 거절되었습니다 (non-fast-forward). 자동 복구를 시도합니다...");

                    var recoveryRebase = await _gitService.PullRebaseAsync(SelectedFolder!);
                    AddLog(recoveryRebase.Output + recoveryRebase.Error);

                    if (recoveryRebase.ExitCode == 0)
                    {
                        AddLog("재푸시 시도 중...");
                        var retryResult = await _gitService.PushAsync(SelectedFolder!, false, isFirstPush);
                        AddLog(retryResult.Output + retryResult.Error);
                        pushResult = retryResult;
                    }
                    else
                    {
                        _dialogService.ShowMessage(
                            "자동 복구(rebase) 중 충돌이 발생했습니다.\n" +
                            "충돌을 수동으로 해결한 후 다시 푸시해주세요.\n\n" +
                            recoveryRebase.Error, "복구 실패");
                        return;
                    }
                }

                if (pushResult.ExitCode == 0)
                {
                    AddLog("✅ Push 성공!");
                    await RefreshRepositoryInfoAsync();
                }
                else if (IsNonFastForward(pushResult.Error))
                {
                    // 자동 복구까지 실패한 경우에만 사용자에게 선택권 제공
                    _dialogService.ShowMessage(
                        "푸시가 계속 거절되고 있습니다.\n\n" +
                        "원격 이력이 로컬과 크게 달라진 상태입니다.\n" +
                        "'강제 푸시'를 체크하면 --force-with-lease로 안전하게 덮어쓸 수 있지만,\n" +
                        "원격의 다른 커밋이 사라질 수 있으니 신중히 결정하세요.\n\n" +
                        pushResult.Error, "푸시 거절");
                }
                else
                {
                    _dialogService.ShowMessage($"Push 중 오류가 발생했습니다: {pushResult.Error}", "오류");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ▼▼▼ [추가] non-fast-forward 오류 감지 헬퍼 ▼▼▼
        private static bool IsNonFastForward(string errorOutput)
        {
            return errorOutput.Contains("non-fast-forward", StringComparison.OrdinalIgnoreCase)
                || errorOutput.Contains("fetch first", StringComparison.OrdinalIgnoreCase)
                || errorOutput.Contains("rejected", StringComparison.OrdinalIgnoreCase)
                || errorOutput.Contains("stale info", StringComparison.OrdinalIgnoreCase);
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲
        // ▲▲▲ [수정] 안전 푸시 파이프라인 여기까지 ▲▲▲

        private bool CanCommit() => IsRepoValid && !string.IsNullOrWhiteSpace(CommitTitle);

        [RelayCommand(CanExecute = nameof(CanCommit))]
        private async Task CommitAsync()
        {
            IsBusy = true;
            AddLog("변경 사항 스테이징 중...");

            // index.lock 자동 복구 헬퍼를 통해 스테이징
            bool isStaged = await TryStageWithLockFixAsync();
            if (!isStaged)
            {
                AddLog("❌ 스테이징에 실패하여 커밋을 중단합니다.");
                IsBusy = false;
                return;
            }

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
    }
}
