// 파일 경로: ChabunGit/UI/Forms/GitignoreEditForm.cs

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChabunGit.UI.Forms
{
    public partial class GitignoreEditForm : Form
    {
        private readonly string _gitignorePath;

        public GitignoreEditForm(string repoPath)
        {
            InitializeComponent();
            // .gitignore 파일의 전체 경로를 설정합니다.
            _gitignorePath = Path.Combine(repoPath, ".gitignore");
            // 폼이 로드될 때 파일 내용을 비동기적으로 불러옵니다.
            this.Load += async (s, e) => await LoadGitignoreContentAsync();
        }

        /// <summary>
        /// .gitignore 파일의 내용을 읽어와 텍스트 박스에 표시합니다.
        /// </summary>
        private async Task LoadGitignoreContentAsync()
        {
            if (File.Exists(_gitignorePath))
            {
                try
                {
                    txtGitignoreContent.Text = await File.ReadAllTextAsync(_gitignorePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($".gitignore 파일을 읽는 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // 파일이 없으면, 주석으로 간단한 사용법을 안내합니다.
                txtGitignoreContent.Text = "# 무시할 파일이나 폴더의 이름을 한 줄에 하나씩 입력하세요.\n" +
                                           "# 예시:\n" +
                                           "# bin/\n" +
                                           "# obj/\n" +
                                           "# *.db\n" +
                                           "# *.log\n";
            }
        }

        /// <summary>
        /// 저장 버튼 클릭 시, 현재 텍스트 박스의 내용을 .gitignore 파일에 덮어씁니다.
        /// </summary>
        private async void btnSaveChanges_Click(object sender, EventArgs e)
        {
            try
            {
                await File.WriteAllTextAsync(_gitignorePath, txtGitignoreContent.Text);
                MessageBox.Show(".gitignore 파일이 성공적으로 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK; // 저장 성공 시 OK 반환
                this.Close(); // 폼 닫기
            }
            catch (Exception ex)
            {
                MessageBox.Show($".gitignore 파일을 저장하는 중 오류가 발생했습니다:\n{ex.Message}", "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 취소 버튼 클릭 시, 변경 사항을 저장하지 않고 폼을 닫습니다.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            // 사용자가 내용을 변경했는지 확인하고, 변경 사항이 있으면 경고 메시지를 표시할 수 있습니다.
            // (간단한 구현을 위해 이 부분은 생략)
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}