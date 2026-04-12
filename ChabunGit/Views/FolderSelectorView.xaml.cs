// File: ChabunGit/Views/FolderSelectorView.xaml.cs
using ChabunGit.ViewModels;
using System.Windows;

namespace ChabunGit.Views
{
    public partial class FolderSelectorView : Window
    {
        public FolderSelectorView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is FolderSelectorViewModel vm)
                {
                    vm.RequestClose += () => this.DialogResult = true;
                }
            };
        }
    }
}
