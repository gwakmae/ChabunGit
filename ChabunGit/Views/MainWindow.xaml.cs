// File: ChabunGit/Views/MainWindow.xaml.cs
using ChabunGit.ViewModels;
using System.Windows;
using System.Windows.Controls;

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
    }
}