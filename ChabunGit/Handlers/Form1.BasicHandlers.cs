// 파일 경로: ChabunGit/Handlers/Form1.BasicHandlers.cs (수정된 코드)

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChabunGit
{
    public partial class Form1
    {
        // InitializeHandlers 메서드를 완전히 삭제합니다.

        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog { Description = "ChabunGit으로 관리할 프로젝트 폴더를 선택하세요." })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    await SelectProjectFolder(fbd.SelectedPath);
                }
            }
        }

        private void txtCommitTitle_TextChanged(object sender, EventArgs e)
        {
            GetControl<Label>("labelTitleLimit").Text = $"제목 ({GetControl<TextBox>("txtCommitTitle").Text.Length}/50)";
            UpdateAllUIState();
        }

        private void listCommitHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAllUIState();
            var listCommitHistory = GetControl<ListBox>("listCommitHistory");
            if (listCommitHistory.SelectedIndex != -1 && _commitHashes.Count > listCommitHistory.SelectedIndex) // 안전장치 추가
            {
                GetControl<TextBox>("txtCommitHashToReset").Text = _commitHashes[listCommitHistory.SelectedIndex];
            }
        }

        private void listChangedFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 현재는 특별한 동작 없음
        }

        private void btnCopyLog_Click(object sender, EventArgs e)
        {
            var txtLog = GetControl<TextBox>("txtLog");
            if (!string.IsNullOrWhiteSpace(txtLog.Text))
            {
                Clipboard.SetText(txtLog.Text);
                _uiManager.AddLog("✅ 로그 내용이 클립보드에 복사되었습니다.");
            }
        }
    }
}