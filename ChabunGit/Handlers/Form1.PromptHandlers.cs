// 파일 경로: ChabunGit/Handlers/Form1.PromptHandlers.cs

using ChabunGit.UI.Forms;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChabunGit
{
    public partial class Form1
    {
        private async void btnGenerateCommitPrompt_Click(object sender, EventArgs e)
        {
            if (!IsRepoSelectedAndValid()) return;
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("AI 질문지를 만드는 중...");

            string prompt;
            string formTitle;
            if (GetControl<ListBox>("listCommitHistory").Items.Count == 0)
            {
                formTitle = "첫 커밋 메시지 질문지 생성";
                prompt = await _promptService.CreateInitialCommitPromptAsync(_selectedFolder!);
            }
            else
            {
                formTitle = "변경 사항 커밋 메시지 질문지 생성";
                prompt = await _promptService.CreateCommitMessagePromptAsync(_selectedFolder!);
            }
            this.Cursor = Cursors.Default;
            _uiManager.AddLog("AI 질문지 생성 완료.");
            using (var promptForm = new PromptDisplayForm(formTitle, prompt)) { promptForm.ShowDialog(this); }
        }

        private async void btnGenerateGitignorePrompt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder)) { MessageBox.Show("폴더를 먼저 선택하세요."); return; }
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog(".gitignore 생성을 위한 AI 질문지를 만드는 중...");
            string prompt = await _promptService.CreateGitignorePromptAsync(_selectedFolder!);
            this.Cursor = Cursors.Default;
            _uiManager.AddLog("AI 질문지 생성 완료.");
            using (var promptForm = new PromptDisplayForm(".gitignore 질문지 생성", prompt)) { promptForm.ShowDialog(this); }
        }

        private async void btnEditGitignore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder)) { MessageBox.Show("폴더를 먼저 선택하세요."); return; }
            using (var editForm = new GitignoreEditForm(_selectedFolder))
            {
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    _uiManager.AddLog(".gitignore 파일이 수정되었습니다. 변경 파일 목록을 갱신합니다.");
                    await RefreshRepositoryInfoAsync();
                }
            }
        }
    }
}