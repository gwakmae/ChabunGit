// File: ChabunGit/Services/WpfDialogService.cs
using ChabunGit.Services.Abstractions;
using ChabunGit.ViewModels;
using ChabunGit.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace ChabunGit.Services
{
    public class WpfDialogService : IDialogService
    {
        // ▼▼▼ [추가] IPromptService와 같은 다른 서비스를 가져오기 위해 IServiceProvider를 주입받습니다. ▼▼▼
        private readonly IServiceProvider _serviceProvider;

        public WpfDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲


        public string? ShowFolderBrowserDialog(string description)
        {
            var dialog = new OpenFolderDialog { Title = description };
            return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FolderName : null;
        }

        public void ShowMessage(string message, string caption)
        {
            MessageBox.Show(Application.Current.MainWindow, message, caption, MessageBoxButton.OK);
        }

        public bool ShowConfirmation(string message, string caption)
        {
            return MessageBox.Show(Application.Current.MainWindow, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
        
        // ▼▼▼ [수정] isForCommitAi 플래그에 따라 다른 ViewModel을 생성하도록 로직을 수정합니다. ▼▼▼
        public void ShowPrompt(string title, string promptText, bool isForCommitAi = false)
        {
            PromptDisplayViewModel viewModel;

            if (isForCommitAi)
            {
                // AI 커밋 프롬프트 생성이 필요한 경우, IPromptService를 동적으로 가져와 ViewModel에 전달합니다.
                var promptService = _serviceProvider.GetRequiredService<IPromptService>();
                viewModel = new PromptDisplayViewModel(promptText, promptService);
            }
            else
            {
                // 일반적인 텍스트 표시의 경우, 기존 방식대로 ViewModel을 생성합니다.
                viewModel = new PromptDisplayViewModel(promptText);
            }
            
            var promptView = new PromptDisplayView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
                // Title은 이제 ViewModel의 속성에 바인딩되므로 여기서는 설정하지 않습니다.
            };
            promptView.ShowDialog();
        }
        // ▲▲▲ [수정] 여기까지 ▲▲▲

        public string? ShowGitignoreEditor(string initialContent)
        {
            var editorView = new GitignoreEditView
            {
                Owner = Application.Current.MainWindow
            };
            var viewModel = new GitignoreEditViewModel(initialContent);
            editorView.DataContext = viewModel;

            if (editorView.ShowDialog() == true)
            {
                return viewModel.Content;
            }
            return null;
        }

        public void ShowCommitDetails(string commitHash, string commitDetails)
        {
            var detailView = new CommitDetailView
            {
                Owner = Application.Current.MainWindow,
                DataContext = new CommitDetailViewModel(commitHash, commitDetails)
            };
            detailView.ShowDialog();
        }
    }
}