// 파일 경로: ChabunGit/Form1.cs (최종 수정본)

using ChabunGit.Core;
using ChabunGit.Models;
using ChabunGit.Services;
using ChabunGit.UI.Managers;
using ChabunGit.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChabunGit
{
    public partial class Form1 : Form
    {
        // --- 멤버 변수 선언 ---
        private string? _selectedFolder;
        private readonly ConfigManager _configManager;
        private readonly UIManager _uiManager;
        private readonly GitService _gitService;
        private readonly PromptService _promptService;
        private List<string> _commitHashes = new List<string>();
        private bool _isNewProjectGuideActive = false;

        public Form1()
        {
            InitializeComponent();

            // 의존성 주입
            _configManager = new ConfigManager();
            _uiManager = new UIManager(this);
            var gitExecutor = new GitCommandExecutor();
            _gitService = new GitService(gitExecutor);
            _promptService = new PromptService(gitExecutor);

            // Form이 로드될 때 비동기 초기화 수행
            this.Load += async (s, e) => await InitializeFormAsync();
        }

        // 다른 클래스가 Form의 컨트롤에 접근할 수 있도록 public 메서드 제공
        public T GetControl<T>(string name) where T : Control
        {
            var controls = this.Controls.Find(name, true);
            if (controls.Length == 0)
            {
                throw new System.Exception($"컨트롤 '{name}'을(를) 찾을 수 없습니다. Form1.Designer.cs에 해당 이름의 컨트롤이 있는지 확인하세요.");
            }
            return (T)controls[0];
        }

        #region 초기화 및 상태 관리 (Form1의 핵심 책임)

        private async Task InitializeFormAsync()
        {
            var settings = await _configManager.LoadConfigAsync();
            if (!string.IsNullOrEmpty(settings.LastOpenedFolderPath) && Directory.Exists(settings.LastOpenedFolderPath))
            {
                await SelectProjectFolder(settings.LastOpenedFolderPath);
            }
            else
            {
                UpdateAllUIState();
            }
        }

        private async Task SelectProjectFolder(string folderPath)
        {
            _selectedFolder = folderPath;
            GetControl<TextBox>("txtSelectedFolder").Text = folderPath;
            await _configManager.SaveConfigAsync(new AppSettings { LastOpenedFolderPath = folderPath });

            if (_gitService.IsGitRepository(folderPath))
            {
                ShowMainGitInterface();
                await RefreshRepositoryInfoAsync();
            }
            else
            {
                ShowNewProjectGuide();
            }
        }

        private async Task RefreshRepositoryInfoAsync()
        {
            if (string.IsNullOrEmpty(_selectedFolder)) return;
            this.Cursor = Cursors.WaitCursor;
            _uiManager.AddLog("저장소 정보를 갱신합니다...");

            var branchResult = await _gitService.Executor.ExecuteAsync(_selectedFolder, "rev-parse --abbrev-ref HEAD");
            GetControl<Label>("labelCurrentBranch").Text = branchResult.ExitCode == 0 ? $"현재 브랜치: {branchResult.Output.Trim()}" : "현재 브랜치: N/A";

            var statusResult = await _gitService.GetChangedFilesAsync(_selectedFolder);
            _uiManager.UpdateChangedFilesList(statusResult.Output);

            var historyResult = await _gitService.GetCommitHistoryAsync(_selectedFolder);
            _uiManager.UpdateCommitHistoryList(historyResult.Output, _commitHashes);

            GetControl<Label>("lblFetchStatus").Text = "원격 저장소 상태를 확인하려면 [Fetch] 버튼을 누르세요.";

            UpdateAllUIState();
            this.Cursor = Cursors.Default;
            _uiManager.AddLog("정보 갱신 완료.");
        }

        private void UpdateAllUIState()
        {
            bool isGitRepo = !string.IsNullOrEmpty(_selectedFolder) && _gitService.IsGitRepository(_selectedFolder);
            int commitCount = GetControl<ListBox>("listCommitHistory").Items.Count;
            int selectedCommitIndex = GetControl<ListBox>("listCommitHistory").SelectedIndex;
            string fetchStatus = GetControl<Label>("lblFetchStatus").Text;
            string commitTitle = GetControl<TextBox>("txtCommitTitle").Text;

            _uiManager.UpdateUIState(_isNewProjectGuideActive, isGitRepo, commitCount, selectedCommitIndex, fetchStatus, commitTitle);
        }

        private void ShowNewProjectGuide()
        {
            _isNewProjectGuideActive = true;
            _uiManager.SetupNewProjectGuideUI();
            UpdateAllUIState();
        }

        private void ShowMainGitInterface()
        {
            _isNewProjectGuideActive = false;
            UpdateAllUIState();
        }

        private bool IsRepoSelectedAndValid()
        {
            if (string.IsNullOrEmpty(_selectedFolder) || !_gitService.IsGitRepository(_selectedFolder))
            {
                MessageBox.Show("먼저 유효한 Git 저장소 폴더를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        #endregion
    }
}