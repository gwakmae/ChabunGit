// File: ChabunGit/ViewModels/PromptDisplayViewModel.cs
using ChabunGit.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class PromptDisplayViewModel : ViewModelBase
    {
        // ▼▼▼ [추가] AI 프롬프트 생성을 위해 IPromptService를 저장할 필드를 추가합니다. ▼▼▼
        private readonly IPromptService? _promptService;

        [ObservableProperty]
        private string _promptText;

        [ObservableProperty]
        private string _copyButtonText = "클립보드로 복사하기";

        // ▼▼▼ [추가] 창 제목과 AI 프롬프트 생성 버튼의 표시 여부를 제어하기 위한 속성을 추가합니다. ▼▼▼
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isGenerateButtonVisible;
        // ▲▲▲ [추가] 여기까지 ▲▲▲


        // 일반 텍스트 표시를 위한 기존 생성자
        public PromptDisplayViewModel(string promptText)
        {
            _promptText = promptText;
            _promptService = null;
            IsGenerateButtonVisible = false;
            Title = "AI 프롬프트"; // 기본 창 제목
        }

        // ▼▼▼ [추가] 변경점 분석 및 AI 프롬프트 생성을 위한 새로운 생성자를 추가합니다. ▼▼▼
        public PromptDisplayViewModel(string diffText, IPromptService promptService)
        {
            _promptText = diffText;
            _promptService = promptService;
            IsGenerateButtonVisible = true; // AI 프롬프트 생성 버튼을 표시
            Title = "변경점 분석 결과"; // 초기 창 제목
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲

        [RelayCommand]
        private void CopyToClipboard()
        {
            try
            {
                Clipboard.SetText(PromptText);
                CopyButtonText = "✅ 복사 완료!";
            }
            catch (System.Exception ex)
            {
                CopyButtonText = "❌ 복사 실패";
                MessageBox.Show($"클립보드에 복사하는 중 오류가 발생했습니다.\n다른 프로그램이 클립보드를 사용 중일 수 있습니다. 잠시 후 다시 시도해주세요.\n\n오류: {ex.Message}",
                                 "복사 오류",
                                 MessageBoxButton.OK,
                                 MessageBoxImage.Error);
            }
        }

        // ▼▼▼ [추가] AI 커밋 메시지 프롬프트를 생성하는 새 커맨드를 추가합니다. ▼▼▼
        [RelayCommand(CanExecute = nameof(IsGenerateButtonVisible))]
        private void GenerateAiPrompt()
        {
            if (_promptService == null) return;

            // diff 내용을 기반으로 최종 AI 프롬프트를 생성합니다.
            string finalPrompt = _promptService.CreateCommitPrompt(PromptText);
            
            // 창의 내용을 최종 프롬프트로 교체합니다.
            PromptText = finalPrompt;

            // 버튼을 숨기고 창 제목을 변경합니다.
            IsGenerateButtonVisible = false;
            Title = "생성된 AI 질문지 (복사하여 사용)";
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲
    }
}