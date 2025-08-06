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
            try
            {
                // 1. 클립보드에 텍스트를 설정하는 위험할 수 있는 작업을 시도합니다.
                Clipboard.SetText(PromptText);

                // 2. 성공했을 경우에만 버튼 텍스트를 변경합니다.
                CopyButtonText = "✅ 복사 완료!";
            }
            catch (Exception ex)
            {
                // 3. 만약 클립보드 접근에 실패하면, 프로그램이 멈추는 대신 사용자에게 알려줍니다.
                CopyButtonText = "❌ 복사 실패";
                MessageBox.Show($"클립보드에 복사하는 중 오류가 발생했습니다.\n다른 프로그램이 클립보드를 사용 중일 수 있습니다. 잠시 후 다시 시도해주세요.\n\n오류: {ex.Message}",
                                "복사 오류",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
} 