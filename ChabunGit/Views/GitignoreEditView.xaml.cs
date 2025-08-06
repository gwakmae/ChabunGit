// File: ChabunGit/Views/GitignoreEditView.xaml.cs
using System.Windows;

namespace ChabunGit.Views
{
    public partial class GitignoreEditView : Window
    {
        public GitignoreEditView()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}