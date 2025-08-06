// File: ChabunGit/Services/WpfDialogService.cs
using ChabunGit.Services.Abstractions;
using ChabunGit.ViewModels;
using ChabunGit.Views;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace ChabunGit.Services
{
    public class WpfDialogService : IDialogService
    {
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

        public void ShowPrompt(string title, string promptText)
        {
            var promptView = new PromptDisplayView
            {
                Owner = Application.Current.MainWindow,
                Title = title,
                DataContext = new PromptDisplayViewModel(promptText)
            };
            promptView.ShowDialog();
        }

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
    }
}