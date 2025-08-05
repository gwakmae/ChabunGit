// 파일 경로: ChabunGit/UI/Forms/PromptDisplayForm.cs

using System.Windows.Forms;

namespace ChabunGit.UI.Forms
{
    public partial class PromptDisplayForm : Form
    {
        public PromptDisplayForm(string title, string promptText)
        {
            InitializeComponent();
            this.Text = title; // 폼의 제목을 설정
            txtPromptContent.Text = promptText; // 표시할 프롬프트 텍스트를 설정

            // Form이 로드될 때 모든 텍스트를 선택하여 사용자가 바로 복사(Ctrl+C)할 수 있도록 합니다.
            this.Load += (sender, e) =>
            {
                txtPromptContent.SelectAll();
                txtPromptContent.Focus();
            };
        }

        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPromptContent.Text))
            {
                Clipboard.SetText(txtPromptContent.Text);
                // 사용자에게 복사되었음을 알리고, 버튼 텍스트를 잠시 변경합니다.
                btnCopyToClipboard.Text = "✅ 복사 완료!";
                this.DialogResult = DialogResult.OK; // 확인 용도로 DialogResult 설정 가능
                // 짧은 시간 후에 폼을 닫거나, 사용자가 직접 닫도록 둡니다.
            }
        }
    }
}