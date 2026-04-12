// File: ChabunGit/Services/WpfDialogService.cs
using ChabunGit.Services.Abstractions;
using ChabunGit.ViewModels;
using ChabunGit.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ChabunGit.Services
{
    public class WpfDialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public WpfDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string? ShowFolderBrowserDialog(string description)
        {
            var dialog = new OpenFolderDialog { Title = description };
            return dialog.ShowDialog(Application.Current.MainWindow) == true
                ? dialog.FolderName
                : null;
        }

        public void ShowMessage(string message, string caption)
        {
            MessageBox.Show(Application.Current.MainWindow, message, caption, MessageBoxButton.OK);
        }

        public bool ShowConfirmation(string message, string caption)
        {
            return MessageBox.Show(
                Application.Current.MainWindow, message, caption,
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void ShowPrompt(string title, string promptText, bool isForCommitAi = false)
        {
            PromptDisplayViewModel viewModel;

            if (isForCommitAi)
            {
                var promptService = _serviceProvider.GetRequiredService<IPromptService>();
                viewModel = new PromptDisplayViewModel(promptText, promptService);
            }
            else
            {
                viewModel = new PromptDisplayViewModel(promptText);
            }

            var promptView = new PromptDisplayView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };
            promptView.ShowDialog();
        }

        public void ShowAiCommitResult(string aiResultText, Action<string, string> onApply)
        {
            var viewModel = new PromptDisplayViewModel(aiResultText, onApply);
            var promptView = new PromptDisplayView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };
            promptView.ShowDialog();
        }

        public void ShowAiGitignoreResult(string aiResultText, Action<string> onApply)
        {
            var viewModel = new PromptDisplayViewModel(aiResultText, onApply);
            var promptView = new PromptDisplayView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };
            promptView.ShowDialog();
        }

        // ▼▼▼ [추가] 폴더/파일 선택 다이얼로그 ▼▼▼
        public List<string>? ShowFolderSelector(string repoPath)
        {
            var viewModel = new FolderSelectorViewModel(repoPath);
            var view = new FolderSelectorView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };

            view.ShowDialog();

            // SelectedPaths가 비어있어도 빈 리스트 반환 (건너뜀도 정상 흐름)
            return viewModel.SelectedPaths.ToList();
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲

        public string? ShowGitignoreEditor(string initialContent)
        {
            var editorView = new GitignoreEditView
            {
                Owner = Application.Current.MainWindow
            };
            var viewModel = new GitignoreEditViewModel(initialContent);
            editorView.DataContext = viewModel;

            return editorView.ShowDialog() == true ? viewModel.Content : null;
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
