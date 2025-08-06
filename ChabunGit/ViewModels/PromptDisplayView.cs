// File: ChabunGit/ViewModels/PromptDisplayViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class PromptDisplayViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _promptText;

        [ObservableProperty]
        private string _copyButtonText = "클립보드로 복사하기";

        public PromptDisplayViewModel(string promptText)
        {
            _promptText = promptText;
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            Clipboard.SetText(PromptText);
            CopyButtonText = "✅ 복사 완료!";
        }
    }
} 