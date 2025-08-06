using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace ChabunGit.ViewModels  // 이 줄이 누락되어 있었습니다
{
    public partial class CommitDetailViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _commitHash = string.Empty;

        [ObservableProperty]
        private string _commitDetails = string.Empty;

        [ObservableProperty]
        private string _copyButtonText = "클립보드로 복사하기";

        public CommitDetailViewModel(string commitHash, string commitDetails)
        {
            _commitHash = commitHash;
            _commitDetails = commitDetails;
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            try
            {
                Clipboard.SetText(CommitDetails);
                CopyButtonText = "✅ 복사 완료!";
            }
            catch (Exception ex)
            {
                CopyButtonText = "❌ 복사 실패";
                MessageBox.Show($"클립보드 복사 중 오류: {ex.Message}", "복사 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
