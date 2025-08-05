// 파일 경로: ChabunGit/UI/Managers/UIManager.cs

using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ChabunGit.UI.Managers
{
    /// <summary>
    /// Form1의 UI 컨트롤들의 상태(가시성, 활성화 여부, 텍스트 등)를 관리합니다.
    /// </summary>
    public class UIManager
    {
        private readonly Form1 _mainForm;

        // Form1의 인스턴스를 받아와 내부 변수에 저장합니다.
        public UIManager(Form1 mainForm)
        {
            _mainForm = mainForm;
        }

        #region UI 업데이트 메서드

        /// <summary>
        /// 프로그램의 현재 상태에 따라 전체 UI 컨트롤의 상태를 업데이트합니다.
        /// </summary>
        public void UpdateUIState(bool isNewProjectGuideActive, bool isGitRepo, int commitCount, int selectedCommitIndex, string fetchStatus, string commitTitle)
        {
            // 새 프로젝트 가이드 모드 처리
            _mainForm.GetControl<GroupBox>("groupBoxNewProjectGuide").Visible = isNewProjectGuideActive;
            bool mainUiVisible = !isNewProjectGuideActive;

            // 일반 Git 작업 UI 요소들의 가시성
            _mainForm.GetControl<Button>("btnFetch").Visible = mainUiVisible;
            _mainForm.GetControl<Button>("btnPull").Visible = mainUiVisible;
            _mainForm.GetControl<Button>("btnPush").Visible = mainUiVisible;
            _mainForm.GetControl<CheckBox>("chkForcePush").Visible = mainUiVisible;
            _mainForm.GetControl<GroupBox>("groupBoxChangedFiles").Visible = mainUiVisible;
            _mainForm.GetControl<GroupBox>("groupBoxCommitMessage").Visible = mainUiVisible;
            _mainForm.GetControl<GroupBox>("groupBoxGitStatus").Visible = mainUiVisible;
            _mainForm.GetControl<GroupBox>("groupBoxCommitHistory").Visible = mainUiVisible;
            _mainForm.GetControl<GroupBox>("groupBoxLog").Visible = mainUiVisible;
            _mainForm.GetControl<GroupBox>("groupBoxGitignore").Visible = mainUiVisible;

            if (isNewProjectGuideActive) return; // 가이드 모드일 경우 아래 로직은 실행 안 함

            // 일반 모드일 때의 활성화/비활성화 로직
            _mainForm.GetControl<Button>("btnFetch").Enabled = isGitRepo;
            _mainForm.GetControl<Button>("btnPush").Enabled = isGitRepo;
            _mainForm.GetControl<CheckBox>("chkForcePush").Enabled = isGitRepo;
            _mainForm.GetControl<Button>("btnPull").Enabled = isGitRepo && fetchStatus.Contains("새로운 내용");
            _mainForm.GetControl<Button>("btnCommit").Enabled = isGitRepo && !string.IsNullOrWhiteSpace(commitTitle);
            _mainForm.GetControl<Button>("btnUndoLastCommit").Enabled = isGitRepo && commitCount > 0;
            _mainForm.GetControl<Button>("btnResetToCommit").Enabled = isGitRepo && selectedCommitIndex != -1;
            _mainForm.GetControl<Button>("btnGenerateCommitPrompt").Enabled = isGitRepo;
            _mainForm.GetControl<Button>("btnGenerateGitignorePrompt").Enabled = isGitRepo;
            _mainForm.GetControl<Button>("btnEditGitignore").Enabled = isGitRepo;
        }

        /// <summary>
        /// 새 프로젝트 가이드 UI를 초기 상태로 설정합니다.
        /// </summary>
        public void SetupNewProjectGuideUI()
        {
            _mainForm.GetControl<GroupBox>("groupBoxNewProjectGuide").BringToFront();
            _mainForm.GetControl<Button>("btnInitializeGit").Enabled = true;
            _mainForm.GetControl<Label>("labelInitRepoStep").Text = "1. 로컬 Git 저장소를 만듭니다. 이 폴더는 이제 Git으로 관리됩니다.";
            _mainForm.GetControl<Button>("btnCreateRemoteRepo").Enabled = false;
            _mainForm.GetControl<TextBox>("txtGitHubUrl").Text = string.Empty;
            _mainForm.GetControl<TextBox>("txtGitHubUrl").Enabled = false;
            _mainForm.GetControl<Label>("labelRemoteRepoStep").Text = "2. GitHub에 새 저장소를 생성하고, HTTPS 주소를 복사하여 붙여넣으세요.";
            _mainForm.GetControl<Button>("btnCompleteGuide").Enabled = false;
        }

        /// <summary>
        /// 로그 패널에 메시지를 추가합니다.
        /// </summary>
        public void AddLog(string message)
        {
            var txtLog = _mainForm.GetControl<TextBox>("txtLog");
            if (string.IsNullOrWhiteSpace(message)) return;
            
            var lines = message.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                txtLog.AppendText($"[{System.DateTime.Now:HH:mm:ss}] {line.Trim()}{System.Environment.NewLine}");
            }
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// 변경된 파일 목록 리스트 박스를 업데이트합니다.
        /// </summary>
        public void UpdateChangedFilesList(string statusOutput)
        {
            var listChangedFiles = _mainForm.GetControl<ListBox>("listChangedFiles");
            listChangedFiles.Items.Clear();
            var lines = statusOutput.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                listChangedFiles.Items.Add(line.Trim());
            }
        }

        /// <summary>
        /// 커밋 이력 리스트 박스를 업데이트합니다.
        /// </summary>
        public void UpdateCommitHistoryList(string historyOutput, List<string> commitHashes)
        {
            var listCommitHistory = _mainForm.GetControl<ListBox>("listCommitHistory");
            listCommitHistory.Items.Clear();
            commitHashes.Clear();

            var lines = historyOutput.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length == 3)
                {
                    listCommitHistory.Items.Add($"{parts[1].Trim()} - {parts[2].Trim()} ({parts[0].Trim()})");
                    commitHashes.Add(parts[0].Trim());
                }
            }
        }
        
        /// <summary>
        /// 커밋 메시지 입력란을 초기화합니다.
        /// </summary>
        public void ClearCommitMessage()
        {
            _mainForm.GetControl<TextBox>("txtCommitTitle").Clear();
            _mainForm.GetControl<TextBox>("txtCommitBody").Clear();
        }

        #endregion
    }
}