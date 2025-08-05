// 파일 경로: ChabunGit/Handlers/Form1.GuideHandlers.cs

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChabunGit
{
    public partial class Form1
    {
        private async void btnInitializeGit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder)) return;
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("Git 저장소 초기화 중...");

            var result = await _gitService.InitRepositoryAsync(_selectedFolder!);
            _uiManager.AddLog(result.Output + result.Error);

            if(result.ExitCode == 0)
            {
                _uiManager.AddLog("✅ Git 저장소 초기화 성공!");
                GetControl<Label>("labelInitRepoStep").Text = "1. ✅ Git 저장소 초기화 완료!";
                GetControl<Button>("btnInitializeGit").Enabled = false;
                GetControl<Button>("btnCreateRemoteRepo").Enabled = true;
                GetControl<TextBox>("txtGitHubUrl").Enabled = true;
            }
            this.Cursor = Cursors.Default;
        }

        private async void btnCreateRemoteRepo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder) || string.IsNullOrWhiteSpace(GetControl<TextBox>("txtGitHubUrl").Text))
            {
                MessageBox.Show("GitHub 저장소 주소를 입력해주세요."); return;
            }
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("원격 저장소 연결 중...");

            var result = await _gitService.AddRemoteAsync(_selectedFolder!, GetControl<TextBox>("txtGitHubUrl").Text.Trim());
            _uiManager.AddLog(result.Output + result.Error);

            if(result.ExitCode == 0)
            {
                _uiManager.AddLog("✅ 원격 저장소 연결 성공!");
                GetControl<Label>("labelRemoteRepoStep").Text = "2. ✅ GitHub 저장소 연결 완료!";
                GetControl<Button>("btnCreateRemoteRepo").Enabled = false;
                GetControl<TextBox>("txtGitHubUrl").Enabled = false;
                GetControl<Button>("btnCompleteGuide").Enabled = true;
            }
            this.Cursor = Cursors.Default;
        }

        private async void btnCompleteGuide_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder)) return;
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("주 브랜치를 'main'으로 설정 중...");

            await _gitService.SetMainBranchAsync(_selectedFolder!);
            ShowMainGitInterface();
            await RefreshRepositoryInfoAsync();

            this.Cursor = Cursors.Default;
        }
    }
}