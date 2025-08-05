// 파일 경로: ChabunGit/Handlers/Form1.GitCommandHandlers.cs

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChabunGit
{
    public partial class Form1
    {
        private async void btnCommit_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid()) return;
            this.Cursor = Cursors.WaitCursor;

            _uiManager.AddLog("변경 사항 스테이징 중...");
            var stageResult = await _gitService.StageAllChangesAsync(_selectedFolder!);
            _uiManager.AddLog(stageResult.Output + stageResult.Error);
            if (stageResult.ExitCode != 0) { this.Cursor = Cursors.Default; return; }

            _uiManager.AddLog("커밋 생성 중...");
            string title = GetControl<TextBox>("txtCommitTitle").Text;
            string body = GetControl<TextBox>("txtCommitBody").Text;
            var commitResult = await _gitService.CommitAsync(_selectedFolder!, title, body);
            _uiManager.AddLog(commitResult.Output + commitResult.Error);

            if (commitResult.ExitCode == 0)
            {
                _uiManager.AddLog("✅ 커밋 성공!");
                _uiManager.ClearCommitMessage();
                await RefreshRepositoryInfoAsync();
            }
            
            this.Cursor = Cursors.Default;
        }

        private async void btnFetch_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid()) return;
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("원격 저장소 확인 중 (Fetch)...");

            var fetchResult = await _gitService.FetchAsync(_selectedFolder!);
            _uiManager.AddLog(fetchResult.Output + fetchResult.Error);

            var statusResult = await _gitService.Executor.ExecuteAsync(_selectedFolder!, "status -sb");
            string status = statusResult.Output.Trim();

            var lblFetchStatus = GetControl<Label>("lblFetchStatus");
            if (status.Contains("behind")) { lblFetchStatus.Text = "⚠️ 경고: 팀원이 올린 새로운 내용이 있습니다. Pull 하세요."; }
            else if (status.Contains("ahead")) { lblFetchStatus.Text = "✅ 원격 저장소보다 앞서 있습니다. Push 하세요."; }
            else if (status.Contains("up-to-date") || !status.Contains("origin")) { lblFetchStatus.Text = "✅ 원격 저장소와 동기화됨."; }
            else { lblFetchStatus.Text = "원격 저장소 상태를 확인할 수 없습니다."; }

            UpdateAllUIState();
            this.Cursor = Cursors.Default;
        }

        private async void btnPull_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid()) return;
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("원격 내용 가져오는 중 (Pull)...");

            var pullResult = await _gitService.PullAsync(_selectedFolder!);
            _uiManager.AddLog(pullResult.Output + pullResult.Error);

            if (pullResult.ExitCode == 0)
            {
                _uiManager.AddLog("✅ Pull 성공!");
                await RefreshRepositoryInfoAsync();
            }
            else
            {
                MessageBox.Show($"Pull 중 오류가 발생했습니다.\n로그를 확인하고 충돌을 수동으로 해결해주세요.\n\n{pullResult.Error}", "Pull 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            this.Cursor = Cursors.Default;
        }

        private async void btnPush_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid()) return;
            bool isForce = GetControl<CheckBox>("chkForcePush").Checked;

            if (isForce)
            {
                var confirm = MessageBox.Show("경고: 강제 푸시는 원격 저장소의 이력을 덮어씁니다. 정말 진행하시겠습니까?", "강제 푸시 경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.No) return;
            }

            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("원격 저장소에 공유 중 (Push)...");
            
            var remoteResult = await _gitService.Executor.ExecuteAsync(_selectedFolder!, "branch -vv");
            bool isFirstPush = !remoteResult.Output.Contains("[origin/");
            
            var pushResult = await _gitService.PushAsync(_selectedFolder!, isForce, isFirstPush);
            _uiManager.AddLog(pushResult.Output + pushResult.Error);
            
            if (pushResult.ExitCode == 0) {
                _uiManager.AddLog("✅ Push 성공!");
                await RefreshRepositoryInfoAsync();
            } else {
                 MessageBox.Show($"Push 중 오류가 발생했습니다: {pushResult.Error}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            this.Cursor = Cursors.Default;
        }

        private async void btnUndoLastCommit_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid()) return;
            var confirm = MessageBox.Show("마지막 커밋을 취소하시겠습니까? (이 커밋으로 변경된 내용은 남아있습니다)", "커밋 취소", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.No) return;
            
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("마지막 커밋 취소 중 (soft reset)...");
            
            var result = await _gitService.ResetLastCommitAsync(_selectedFolder!);
            _uiManager.AddLog(result.Output + result.Error);
            
            await RefreshRepositoryInfoAsync();
            this.Cursor = Cursors.Default;
        }

        private async void btnResetToCommit_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid() || GetControl<ListBox>("listCommitHistory").SelectedIndex == -1) return;
            string selectedHash = _commitHashes[GetControl<ListBox>("listCommitHistory").SelectedIndex];
            
            var confirm = MessageBox.Show($"경고: {selectedHash} 시점으로 돌아가면 이후의 모든 커밋과 현재 변경 사항이 사라집니다. 정말 진행하시겠습니까?", "과거로 돌아가기", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.No) return;

            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog($"{selectedHash} 시점으로 돌아가는 중 (hard reset)...");

            var result = await _gitService.ResetToCommitAsync(_selectedFolder!, selectedHash);
            _uiManager.AddLog(result.Output + result.Error);

            await RefreshRepositoryInfoAsync();
            this.Cursor = Cursors.Default;
        }
    }
}