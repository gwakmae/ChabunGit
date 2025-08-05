// 파일 경로: ChabunGit/Form1.Designer.cs

// --- 이 파일은 Visual Studio Designer에 의해 자동으로 생성/관리됩니다. ---
// --- 아래 내용은 Desinger가 생성할 코드의 예시이며, 실제 사용 시에는 ---
// --- Visual Studio의 Designer 기능을 통해 관리하는 것이 일반적입니다. ---

namespace ChabunGit
{
    partial class Form1
    {
        // 필수 디자이너 변수입니다.
        private System.ComponentModel.IContainer components = null;

        // 사용 중인 모든 리소스를 정리합니다.
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // 디자이너 지원에 필요한 메서드입니다.
        // 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        private void InitializeComponent()
        {
            this.btnSelectFolder = new System.Windows.Forms.Button();
            this.txtSelectedFolder = new System.Windows.Forms.TextBox();
            this.labelCurrentBranch = new System.Windows.Forms.Label();
            this.btnFetch = new System.Windows.Forms.Button();
            this.btnPull = new System.Windows.Forms.Button();
            this.btnPush = new System.Windows.Forms.Button();
            this.chkForcePush = new System.Windows.Forms.CheckBox();
            this.groupBoxChangedFiles = new System.Windows.Forms.GroupBox();
            this.listChangedFiles = new System.Windows.Forms.ListBox();
            this.groupBoxCommitMessage = new System.Windows.Forms.GroupBox();
            this.btnCommit = new System.Windows.Forms.Button();
            this.btnGenerateCommitPrompt = new System.Windows.Forms.Button();
            this.txtCommitBody = new System.Windows.Forms.TextBox();
            this.txtCommitTitle = new System.Windows.Forms.TextBox();
            this.labelTitleLimit = new System.Windows.Forms.Label();
            this.groupBoxGitStatus = new System.Windows.Forms.GroupBox();
            this.lblFetchStatus = new System.Windows.Forms.Label();
            this.groupBoxCommitHistory = new System.Windows.Forms.GroupBox();
            this.txtCommitHashToReset = new System.Windows.Forms.TextBox();
            this.btnResetToCommit = new System.Windows.Forms.Button();
            this.btnUndoLastCommit = new System.Windows.Forms.Button();
            this.listCommitHistory = new System.Windows.Forms.ListBox();
            this.groupBoxLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.groupBoxNewProjectGuide = new System.Windows.Forms.GroupBox();
            this.btnCompleteGuide = new System.Windows.Forms.Button();
            this.btnCreateRemoteRepo = new System.Windows.Forms.Button();
            this.txtGitHubUrl = new System.Windows.Forms.TextBox();
            this.labelGitHubUrlPrompt = new System.Windows.Forms.Label();
            this.labelRemoteRepoStep = new System.Windows.Forms.Label();
            this.btnInitializeGit = new System.Windows.Forms.Button();
            this.labelInitRepoStep = new System.Windows.Forms.Label();
            this.groupBoxGitignore = new System.Windows.Forms.GroupBox();
            this.btnGenerateGitignorePrompt = new System.Windows.Forms.Button();
            this.btnEditGitignore = new System.Windows.Forms.Button();
            this.btnCopyLog = new System.Windows.Forms.Button(); // btnCopyLog 선언 추가
            this.groupBoxChangedFiles.SuspendLayout();
            this.groupBoxCommitMessage.SuspendLayout();
            this.groupBoxGitStatus.SuspendLayout();
            this.groupBoxCommitHistory.SuspendLayout();
            this.groupBoxLog.SuspendLayout();
            this.groupBoxNewProjectGuide.SuspendLayout();
            this.groupBoxGitignore.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSelectFolder
            // 
            this.btnSelectFolder.Location = new System.Drawing.Point(12, 12);
            this.btnSelectFolder.Name = "btnSelectFolder";
            this.btnSelectFolder.Size = new System.Drawing.Size(100, 29);
            this.btnSelectFolder.TabIndex = 0;
            this.btnSelectFolder.Text = "폴더 선택";
            this.btnSelectFolder.UseVisualStyleBackColor = true;
            this.btnSelectFolder.Click += new System.EventHandler(this.btnSelectFolder_Click);
            // 
            // txtSelectedFolder
            // 
            this.txtSelectedFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSelectedFolder.Location = new System.Drawing.Point(118, 15);
            this.txtSelectedFolder.Name = "txtSelectedFolder";
            this.txtSelectedFolder.ReadOnly = true;
            this.txtSelectedFolder.Size = new System.Drawing.Size(534, 23);
            this.txtSelectedFolder.TabIndex = 1;
            // 
            // labelCurrentBranch
            // 
            this.labelCurrentBranch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrentBranch.Location = new System.Drawing.Point(658, 16);
            this.labelCurrentBranch.Name = "labelCurrentBranch";
            this.labelCurrentBranch.Size = new System.Drawing.Size(144, 23);
            this.labelCurrentBranch.TabIndex = 2;
            this.labelCurrentBranch.Text = "현재 브랜치: N/A";
            this.labelCurrentBranch.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnFetch
            // 
            this.btnFetch.Location = new System.Drawing.Point(12, 47);
            this.btnFetch.Name = "btnFetch";
            this.btnFetch.Size = new System.Drawing.Size(130, 29);
            this.btnFetch.TabIndex = 3;
            this.btnFetch.Text = "원격 확인 (Fetch)";
            this.btnFetch.UseVisualStyleBackColor = true;
            this.btnFetch.Click += new System.EventHandler(this.btnFetch_Click);
            // 
            // btnPull
            // 
            this.btnPull.Location = new System.Drawing.Point(148, 47);
            this.btnPull.Name = "btnPull";
            this.btnPull.Size = new System.Drawing.Size(130, 29);
            this.btnPull.TabIndex = 4;
            this.btnPull.Text = "원격 가져오기 (Pull)";
            this.btnPull.UseVisualStyleBackColor = true;
            this.btnPull.Click += new System.EventHandler(this.btnPull_Click);
            // 
            // btnPush
            // 
            this.btnPush.Location = new System.Drawing.Point(284, 47);
            this.btnPush.Name = "btnPush";
            this.btnPush.Size = new System.Drawing.Size(130, 29);
            this.btnPush.TabIndex = 5;
            this.btnPush.Text = "GitHub에 공유 (Push)";
            this.btnPush.UseVisualStyleBackColor = true;
            this.btnPush.Click += new System.EventHandler(this.btnPush_Click);
            // 
            // chkForcePush
            // 
            this.chkForcePush.AutoSize = true;
            this.chkForcePush.Location = new System.Drawing.Point(420, 52);
            this.chkForcePush.Name = "chkForcePush";
            this.chkForcePush.Size = new System.Drawing.Size(78, 19);
            this.chkForcePush.TabIndex = 6;
            this.chkForcePush.Text = "강제 푸시";
            this.chkForcePush.UseVisualStyleBackColor = true;
            // 
            // groupBoxChangedFiles
            // 
            this.groupBoxChangedFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxChangedFiles.Controls.Add(this.listChangedFiles);
            this.groupBoxChangedFiles.Location = new System.Drawing.Point(12, 82);
            this.groupBoxChangedFiles.Name = "groupBoxChangedFiles";
            this.groupBoxChangedFiles.Size = new System.Drawing.Size(266, 208);
            this.groupBoxChangedFiles.TabIndex = 7;
            this.groupBoxChangedFiles.TabStop = false;
            this.groupBoxChangedFiles.Text = "변경/추가/삭제된 파일 목록";
            // 
            // listChangedFiles
            // 
            this.listChangedFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listChangedFiles.FormattingEnabled = true;
            this.listChangedFiles.ItemHeight = 15;
            this.listChangedFiles.Location = new System.Drawing.Point(6, 22);
            this.listChangedFiles.Name = "listChangedFiles";
            this.listChangedFiles.Size = new System.Drawing.Size(254, 169);
            this.listChangedFiles.TabIndex = 0;
            this.listChangedFiles.SelectedIndexChanged += new System.EventHandler(this.listChangedFiles_SelectedIndexChanged);
            // 
            // groupBoxCommitMessage
            // 
            this.groupBoxCommitMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxCommitMessage.Controls.Add(this.btnCommit);
            this.groupBoxCommitMessage.Controls.Add(this.btnGenerateCommitPrompt);
            this.groupBoxCommitMessage.Controls.Add(this.txtCommitBody);
            this.groupBoxCommitMessage.Controls.Add(this.txtCommitTitle);
            this.groupBoxCommitMessage.Controls.Add(this.labelTitleLimit);
            this.groupBoxCommitMessage.Location = new System.Drawing.Point(284, 82);
            this.groupBoxCommitMessage.Name = "groupBoxCommitMessage";
            this.groupBoxCommitMessage.Size = new System.Drawing.Size(518, 208);
            this.groupBoxCommitMessage.TabIndex = 8;
            this.groupBoxCommitMessage.TabStop = false;
            this.groupBoxCommitMessage.Text = "커밋 메시지 작성";
            // 
            // btnCommit
            // 
            this.btnCommit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCommit.Location = new System.Drawing.Point(344, 173);
            this.btnCommit.Name = "btnCommit";
            this.btnCommit.Size = new System.Drawing.Size(168, 29);
            this.btnCommit.TabIndex = 4;
            this.btnCommit.Text = "커밋 (내 PC에 저장)";
            this.btnCommit.UseVisualStyleBackColor = true;
            this.btnCommit.Click += new System.EventHandler(this.btnCommit_Click);
            // 
            // btnGenerateCommitPrompt
            // 
            this.btnGenerateCommitPrompt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGenerateCommitPrompt.Location = new System.Drawing.Point(6, 173);
            this.btnGenerateCommitPrompt.Name = "btnGenerateCommitPrompt";
            this.btnGenerateCommitPrompt.Size = new System.Drawing.Size(170, 29);
            this.btnGenerateCommitPrompt.TabIndex = 3;
            this.btnGenerateCommitPrompt.Text = "✨ 커밋 메시지 프롬프트";
            this.btnGenerateCommitPrompt.UseVisualStyleBackColor = true;
            this.btnGenerateCommitPrompt.Click += new System.EventHandler(this.btnGenerateCommitPrompt_Click);
            // 
            // txtCommitBody
            // 
            this.txtCommitBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCommitBody.Location = new System.Drawing.Point(6, 68);
            this.txtCommitBody.Multiline = true;
            this.txtCommitBody.Name = "txtCommitBody";
            this.txtCommitBody.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCommitBody.Size = new System.Drawing.Size(506, 99);
            this.txtCommitBody.TabIndex = 2;
            // 
            // txtCommitTitle
            // 
            this.txtCommitTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCommitTitle.Location = new System.Drawing.Point(6, 39);
            this.txtCommitTitle.MaxLength = 50;
            this.txtCommitTitle.Name = "txtCommitTitle";
            this.txtCommitTitle.Size = new System.Drawing.Size(506, 23);
            this.txtCommitTitle.TabIndex = 1;
            this.txtCommitTitle.TextChanged += new System.EventHandler(this.txtCommitTitle_TextChanged);
            // 
            // labelTitleLimit
            // 
            this.labelTitleLimit.AutoSize = true;
            this.labelTitleLimit.Location = new System.Drawing.Point(6, 21);
            this.labelTitleLimit.Name = "labelTitleLimit";
            this.labelTitleLimit.Size = new System.Drawing.Size(126, 15);
            this.labelTitleLimit.TabIndex = 0;
            this.labelTitleLimit.Text = "제목 (50자 제한) : 0/50";
            // 
            // groupBoxGitStatus
            // 
            this.groupBoxGitStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxGitStatus.Controls.Add(this.lblFetchStatus);
            this.groupBoxGitStatus.Location = new System.Drawing.Point(12, 609);
            this.groupBoxGitStatus.Name = "groupBoxGitStatus";
            this.groupBoxGitStatus.Size = new System.Drawing.Size(790, 50);
            this.groupBoxGitStatus.TabIndex = 9;
            this.groupBoxGitStatus.TabStop = false;
            this.groupBoxGitStatus.Text = "Git 상태";
            // 
            // lblFetchStatus
            // 
            this.lblFetchStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFetchStatus.Location = new System.Drawing.Point(3, 19);
            this.lblFetchStatus.Name = "lblFetchStatus";
            this.lblFetchStatus.Size = new System.Drawing.Size(784, 28);
            this.lblFetchStatus.TabIndex = 0;
            this.lblFetchStatus.Text = "초기화";
            this.lblFetchStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBoxCommitHistory
            // 
            this.groupBoxCommitHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxCommitHistory.Controls.Add(this.txtCommitHashToReset);
            this.groupBoxCommitHistory.Controls.Add(this.btnResetToCommit);
            this.groupBoxCommitHistory.Controls.Add(this.btnUndoLastCommit);
            this.groupBoxCommitHistory.Controls.Add(this.listCommitHistory);
            this.groupBoxCommitHistory.Location = new System.Drawing.Point(284, 296);
            this.groupBoxCommitHistory.Name = "groupBoxCommitHistory";
            this.groupBoxCommitHistory.Size = new System.Drawing.Size(518, 307);
            this.groupBoxCommitHistory.TabIndex = 10;
            this.groupBoxCommitHistory.TabStop = false;
            this.groupBoxCommitHistory.Text = "커밋 이력";
            // 
            // txtCommitHashToReset
            // 
            this.txtCommitHashToReset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCommitHashToReset.Location = new System.Drawing.Point(6, 278);
            this.txtCommitHashToReset.Name = "txtCommitHashToReset";
            this.txtCommitHashToReset.ReadOnly = true;
            this.txtCommitHashToReset.Size = new System.Drawing.Size(506, 23);
            this.txtCommitHashToReset.TabIndex = 3;
            // 
            // btnResetToCommit
            // 
            this.btnResetToCommit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetToCommit.Location = new System.Drawing.Point(344, 243);
            this.btnResetToCommit.Name = "btnResetToCommit";
            this.btnResetToCommit.Size = new System.Drawing.Size(168, 29);
            this.btnResetToCommit.TabIndex = 2;
            this.btnResetToCommit.Text = "이 시점으로 돌아가기";
            this.btnResetToCommit.UseVisualStyleBackColor = true;
            this.btnResetToCommit.Click += new System.EventHandler(this.btnResetToCommit_Click);
            // 
            // btnUndoLastCommit
            // 
            this.btnUndoLastCommit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUndoLastCommit.Location = new System.Drawing.Point(6, 243);
            this.btnUndoLastCommit.Name = "btnUndoLastCommit";
            this.btnUndoLastCommit.Size = new System.Drawing.Size(168, 29);
            this.btnUndoLastCommit.TabIndex = 1;
            this.btnUndoLastCommit.Text = "방금 커밋 취소";
            this.btnUndoLastCommit.UseVisualStyleBackColor = true;
            this.btnUndoLastCommit.Click += new System.EventHandler(this.btnUndoLastCommit_Click);
            // 
            // listCommitHistory
            // 
            this.listCommitHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listCommitHistory.FormattingEnabled = true;
            this.listCommitHistory.ItemHeight = 15;
            this.listCommitHistory.Location = new System.Drawing.Point(6, 22);
            this.listCommitHistory.Name = "listCommitHistory";
            this.listCommitHistory.Size = new System.Drawing.Size(506, 214);
            this.listCommitHistory.TabIndex = 0;
            this.listCommitHistory.SelectedIndexChanged += new System.EventHandler(this.listCommitHistory_SelectedIndexChanged);
            // 
            // groupBoxLog
            // 
            this.groupBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxLog.Controls.Add(this.btnCopyLog);
            this.groupBoxLog.Controls.Add(this.txtLog);
            this.groupBoxLog.Location = new System.Drawing.Point(12, 665);
            this.groupBoxLog.Name = "groupBoxLog";
            this.groupBoxLog.Size = new System.Drawing.Size(790, 110);
            this.groupBoxLog.TabIndex = 11;
            this.groupBoxLog.TabStop = false;
            this.groupBoxLog.Text = "명령어 로그";
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(6, 22);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(778, 52);
            this.txtLog.TabIndex = 0;
            this.txtLog.WordWrap = false;
            // 
            // groupBoxNewProjectGuide
            // 
            this.groupBoxNewProjectGuide.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxNewProjectGuide.Controls.Add(this.btnCompleteGuide);
            this.groupBoxNewProjectGuide.Controls.Add(this.btnCreateRemoteRepo);
            this.groupBoxNewProjectGuide.Controls.Add(this.txtGitHubUrl);
            this.groupBoxNewProjectGuide.Controls.Add(this.labelGitHubUrlPrompt);
            this.groupBoxNewProjectGuide.Controls.Add(this.labelRemoteRepoStep);
            this.groupBoxNewProjectGuide.Controls.Add(this.btnInitializeGit);
            this.groupBoxNewProjectGuide.Controls.Add(this.labelInitRepoStep);
            this.groupBoxNewProjectGuide.Location = new System.Drawing.Point(12, 82);
            this.groupBoxNewProjectGuide.Name = "groupBoxNewProjectGuide";
            this.groupBoxNewProjectGuide.Size = new System.Drawing.Size(790, 521);
            this.groupBoxNewProjectGuide.TabIndex = 12;
            this.groupBoxNewProjectGuide.TabStop = false;
            this.groupBoxNewProjectGuide.Text = "새 프로젝트 설정 가이드";
            this.groupBoxNewProjectGuide.Visible = false;
            // 
            // btnCompleteGuide
            // 
            this.btnCompleteGuide.Enabled = false;
            this.btnCompleteGuide.Location = new System.Drawing.Point(6, 240);
            this.btnCompleteGuide.Name = "btnCompleteGuide";
            this.btnCompleteGuide.Size = new System.Drawing.Size(778, 30);
            this.btnCompleteGuide.TabIndex = 6;
            this.btnCompleteGuide.Text = "설정 완료 및 일반 모드로 돌아가기";
            this.btnCompleteGuide.UseVisualStyleBackColor = true;
            this.btnCompleteGuide.Click += new System.EventHandler(this.btnCompleteGuide_Click);
            // 
            // btnCreateRemoteRepo
            // 
            this.btnCreateRemoteRepo.Location = new System.Drawing.Point(610, 200);
            this.btnCreateRemoteRepo.Name = "btnCreateRemoteRepo";
            this.btnCreateRemoteRepo.Size = new System.Drawing.Size(170, 29);
            this.btnCreateRemoteRepo.TabIndex = 5;
            this.btnCreateRemoteRepo.Text = "원격 저장소 연결하기";
            this.btnCreateRemoteRepo.UseVisualStyleBackColor = true;
            this.btnCreateRemoteRepo.Click += new System.EventHandler(this.btnCreateRemoteRepo_Click);
            // 
            // txtGitHubUrl
            // 
            this.txtGitHubUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGitHubUrl.Location = new System.Drawing.Point(210, 202);
            this.txtGitHubUrl.Name = "txtGitHubUrl";
            this.txtGitHubUrl.Size = new System.Drawing.Size(394, 23);
            this.txtGitHubUrl.TabIndex = 4;
            // 
            // labelGitHubUrlPrompt
            // 
            this.labelGitHubUrlPrompt.Location = new System.Drawing.Point(6, 205);
            this.labelGitHubUrlPrompt.Name = "labelGitHubUrlPrompt";
            this.labelGitHubUrlPrompt.Size = new System.Drawing.Size(200, 20);
            this.labelGitHubUrlPrompt.TabIndex = 3;
            this.labelGitHubUrlPrompt.Text = "GitHub 저장소 HTTPS 주소:";
            // 
            // labelRemoteRepoStep
            // 
            this.labelRemoteRepoStep.Location = new System.Drawing.Point(6, 110);
            this.labelRemoteRepoStep.Name = "labelRemoteRepoStep";
            this.labelRemoteRepoStep.Size = new System.Drawing.Size(778, 90);
            this.labelRemoteRepoStep.TabIndex = 2;
            this.labelRemoteRepoStep.Text = "2. GitHub에 새 저장소를 생성하고, 아래 HTTPS 주소를 복사한 후 여기에 붙여넣으세요. (README, .gitignore 추가는 체크 해제하고 빈 저장소로 생성하세요.)";
            // 
            // btnInitializeGit
            // 
            this.btnInitializeGit.Location = new System.Drawing.Point(6, 70);
            this.btnInitializeGit.Name = "btnInitializeGit";
            this.btnInitializeGit.Size = new System.Drawing.Size(778, 30);
            this.btnInitializeGit.TabIndex = 1;
            this.btnInitializeGit.Text = "좋습니다, Git 저장소로 만들기 (git init)";
            this.btnInitializeGit.UseVisualStyleBackColor = true;
            this.btnInitializeGit.Click += new System.EventHandler(this.btnInitializeGit_Click);
            // 
            // labelInitRepoStep
            // 
            this.labelInitRepoStep.Location = new System.Drawing.Point(6, 25);
            this.labelInitRepoStep.Name = "labelInitRepoStep";
            this.labelInitRepoStep.Size = new System.Drawing.Size(778, 40);
            this.labelInitRepoStep.TabIndex = 0;
            this.labelInitRepoStep.Text = "1. 로컬 Git 저장소를 만듭니다. 이 폴더는 이제 Git으로 관리됩니다.";
            // 
            // groupBoxGitignore
            // 
            this.groupBoxGitignore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxGitignore.Controls.Add(this.btnGenerateGitignorePrompt);
            this.groupBoxGitignore.Controls.Add(this.btnEditGitignore);
            this.groupBoxGitignore.Location = new System.Drawing.Point(12, 781);
            this.groupBoxGitignore.Name = "groupBoxGitignore";
            this.groupBoxGitignore.Size = new System.Drawing.Size(266, 70);
            this.groupBoxGitignore.TabIndex = 13;
            this.groupBoxGitignore.TabStop = false;
            this.groupBoxGitignore.Text = ".gitignore 관리";
            // 
            // btnGenerateGitignorePrompt
            // 
            this.btnGenerateGitignorePrompt.Location = new System.Drawing.Point(135, 22);
            this.btnGenerateGitignorePrompt.Name = "btnGenerateGitignorePrompt";
            this.btnGenerateGitignorePrompt.Size = new System.Drawing.Size(125, 30);
            this.btnGenerateGitignorePrompt.TabIndex = 1;
            this.btnGenerateGitignorePrompt.Text = "✨ .gitignore 질문";
            this.btnGenerateGitignorePrompt.UseVisualStyleBackColor = true;
            this.btnGenerateGitignorePrompt.Click += new System.EventHandler(this.btnGenerateGitignorePrompt_Click);
            // 
            // btnEditGitignore
            // 
            this.btnEditGitignore.Location = new System.Drawing.Point(6, 22);
            this.btnEditGitignore.Name = "btnEditGitignore";
            this.btnEditGitignore.Size = new System.Drawing.Size(125, 30);
            this.btnEditGitignore.TabIndex = 0;
            this.btnEditGitignore.Text = ".gitignore 열기";
            this.btnEditGitignore.UseVisualStyleBackColor = true;
            this.btnEditGitignore.Click += new System.EventHandler(this.btnEditGitignore_Click);
            // 
            // btnCopyLog
            // 
            this.btnCopyLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyLog.Location = new System.Drawing.Point(6, 79);
            this.btnCopyLog.Name = "btnCopyLog";
            this.btnCopyLog.Size = new System.Drawing.Size(778, 25);
            this.btnCopyLog.TabIndex = 1;
            this.btnCopyLog.Text = "로그 복사";
            this.btnCopyLog.UseVisualStyleBackColor = true;
            this.btnCopyLog.Click += new System.EventHandler(this.btnCopyLog_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(814, 863);
            this.Controls.Add(this.groupBoxGitignore);
            this.Controls.Add(this.groupBoxNewProjectGuide);
            this.Controls.Add(this.groupBoxLog);
            this.Controls.Add(this.groupBoxCommitHistory);
            this.Controls.Add(this.groupBoxGitStatus);
            this.Controls.Add(this.groupBoxCommitMessage);
            this.Controls.Add(this.groupBoxChangedFiles);
            this.Controls.Add(this.chkForcePush);
            this.Controls.Add(this.btnPush);
            this.Controls.Add(this.btnPull);
            this.Controls.Add(this.btnFetch);
            this.Controls.Add(this.labelCurrentBranch);
            this.Controls.Add(this.txtSelectedFolder);
            this.Controls.Add(this.btnSelectFolder);
            this.MinimumSize = new System.Drawing.Size(830, 902);
            this.Name = "Form1";
            this.Text = "ChabunGit (차분깃)";
            this.groupBoxChangedFiles.ResumeLayout(false);
            this.groupBoxCommitMessage.ResumeLayout(false);
            this.groupBoxCommitMessage.PerformLayout();
            this.groupBoxGitStatus.ResumeLayout(false);
            this.groupBoxCommitHistory.ResumeLayout(false);
            this.groupBoxCommitHistory.PerformLayout();
            this.groupBoxLog.ResumeLayout(false);
            this.groupBoxLog.PerformLayout();
            this.groupBoxNewProjectGuide.ResumeLayout(false);
            this.groupBoxNewProjectGuide.PerformLayout();
            this.groupBoxGitignore.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // --- UI 컨트롤 멤버 변수 선언 ---
        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.TextBox txtSelectedFolder;
        private System.Windows.Forms.Label labelCurrentBranch;

        private System.Windows.Forms.Button btnFetch;
        private System.Windows.Forms.Button btnPull;
        private System.Windows.Forms.Button btnPush;
        private System.Windows.Forms.CheckBox chkForcePush;

        private System.Windows.Forms.GroupBox groupBoxChangedFiles;
        private System.Windows.Forms.ListBox listChangedFiles;

        private System.Windows.Forms.GroupBox groupBoxCommitMessage;
        private System.Windows.Forms.Label labelTitleLimit;
        private System.Windows.Forms.TextBox txtCommitTitle;
        private System.Windows.Forms.TextBox txtCommitBody;
        private System.Windows.Forms.Button btnGenerateCommitPrompt;
        private System.Windows.Forms.Button btnCommit;

        private System.Windows.Forms.GroupBox groupBoxGitStatus;
        private System.Windows.Forms.Label lblFetchStatus;

        private System.Windows.Forms.GroupBox groupBoxCommitHistory;
        private System.Windows.Forms.ListBox listCommitHistory;
        private System.Windows.Forms.Button btnUndoLastCommit;
        private System.Windows.Forms.Button btnResetToCommit;
        private System.Windows.Forms.TextBox txtCommitHashToReset; // 커밋 해시 표시 텍스트 박스

        private System.Windows.Forms.GroupBox groupBoxLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnCopyLog;

        // 새 프로젝트 가이드 관련 컨트롤
        private System.Windows.Forms.GroupBox groupBoxNewProjectGuide;
        private System.Windows.Forms.Label labelInitRepoStep;
        private System.Windows.Forms.Button btnInitializeGit;
        private System.Windows.Forms.Label labelRemoteRepoStep;
        private System.Windows.Forms.Label labelGitHubUrlPrompt;
        private System.Windows.Forms.TextBox txtGitHubUrl;
        private System.Windows.Forms.Button btnCreateRemoteRepo;
        private System.Windows.Forms.Button btnCompleteGuide;

        // gitignore 관리 섹션
        private System.Windows.Forms.GroupBox groupBoxGitignore;
        private System.Windows.Forms.Button btnEditGitignore;
        private System.Windows.Forms.Button btnGenerateGitignorePrompt;
    }
}