// 파일 경로: ChabunGit/UI/Forms/PromptDisplayForm.Designer.cs

namespace ChabunGit.UI.Forms
{
    partial class PromptDisplayForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtPromptContent;
        private System.Windows.Forms.Button btnCopyToClipboard;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtPromptContent = new System.Windows.Forms.TextBox();
            this.btnCopyToClipboard = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtPromptContent
            // 
            this.txtPromptContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPromptContent.Location = new System.Drawing.Point(12, 12);
            this.txtPromptContent.Multiline = true;
            this.txtPromptContent.Name = "txtPromptContent";
            this.txtPromptContent.ReadOnly = true;
            this.txtPromptContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPromptContent.Size = new System.Drawing.Size(560, 298);
            this.txtPromptContent.TabIndex = 0;
            // 
            // btnCopyToClipboard
            // 
            this.btnCopyToClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyToClipboard.Location = new System.Drawing.Point(12, 316);
            this.btnCopyToClipboard.Name = "btnCopyToClipboard";
            this.btnCopyToClipboard.Size = new System.Drawing.Size(560, 33);
            this.btnCopyToClipboard.TabIndex = 1;
            this.btnCopyToClipboard.Text = "클립보드로 복사하기";
            this.btnCopyToClipboard.UseVisualStyleBackColor = true;
            this.btnCopyToClipboard.Click += new System.EventHandler(this.btnCopyToClipboard_Click);
            // 
            // PromptDisplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.btnCopyToClipboard);
            this.Controls.Add(this.txtPromptContent);
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "PromptDisplayForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "생성된 AI 질문지"; // 이 제목은 Form 생성 시 동적으로 변경됩니다.
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}