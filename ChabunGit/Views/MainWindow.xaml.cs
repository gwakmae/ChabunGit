// File: ChabunGit/Views/MainWindow.xaml.cs
using ChabunGit.Models;
using ChabunGit.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChabunGit.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TextBox의 내용이 변경될 때마다 맨 아래로 스크롤
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }

        private void CommitListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is CommitInfo selectedCommit)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // ShowCommitDetailsCommand를 직접 실행
                    if (viewModel.ShowCommitDetailsCommand.CanExecute(selectedCommit))
                    {
                        viewModel.ShowCommitDetailsCommand.Execute(selectedCommit);
                    }
                }
            }
        }
    }
}
