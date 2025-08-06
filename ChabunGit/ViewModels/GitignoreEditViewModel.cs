// File: ChabunGit/ViewModels/GitignoreEditViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChabunGit.ViewModels
{
    public partial class GitignoreEditViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _content;

        public GitignoreEditViewModel(string initialContent)
        {
            _content = initialContent;
        }
    }
}