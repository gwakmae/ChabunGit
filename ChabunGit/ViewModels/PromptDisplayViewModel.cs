// File: ChabunGit/ViewModels/PromptDisplayViewModel.cs
using ChabunGit.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class PromptDisplayViewModel : ViewModelBase
    {
        private readonly IPromptService? _promptService;
        private readonly Action<string, string>? _onApplyCommit;
        private readonly Action<string>? _onApplyGitignore;

        public event Action? RequestClose;

        [ObservableProperty] private string _promptText;
        [ObservableProperty] private string _copyButtonText = "클립보드로 복사하기";
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private bool _isGenerateButtonVisible;
        [ObservableProperty] private bool _isApplyButtonVisible;
        [ObservableProperty] private string _applyButtonText = "✅ 커밋 창에 적용";
        [ObservableProperty] private string _parsedTitle = string.Empty;
        [ObservableProperty] private string _parsedBody = string.Empty;
        [ObservableProperty] private bool _isManualInputVisible;
        [ObservableProperty] private string _manualTitle = string.Empty;
        [ObservableProperty] private string _manualBody = string.Empty;
        [ObservableProperty] private string _parseWarningText = string.Empty;


        // 생성자 1: 일반 텍스트 표시용
        public PromptDisplayViewModel(string promptText)
        {
            _promptText = promptText;
            IsGenerateButtonVisible = false;
            IsApplyButtonVisible = false;
            IsManualInputVisible = false;
            Title = "AI 프롬프트";
        }

        // 생성자 2: 변경점 분석 → AI 프롬프트 생성용
        public PromptDisplayViewModel(string diffText, IPromptService promptService)
        {
            _promptText = diffText;
            _promptService = promptService;
            IsGenerateButtonVisible = true;
            IsApplyButtonVisible = false;
            IsManualInputVisible = false;
            Title = "변경점 분석 결과";
        }

        // 생성자 3: AI 커밋 메시지 결과 → 커밋 창 적용용
        public PromptDisplayViewModel(string aiResultText, Action<string, string> onApplyCommit)
        {
            _promptText = aiResultText;
            _onApplyCommit = onApplyCommit;
            IsGenerateButtonVisible = false;
            ApplyButtonText = "✅ 커밋 창에 적용";
            Title = "AI가 생성한 커밋 메시지";

            ParseCommitMessage(aiResultText);
        }

        // 생성자 4: AI .gitignore 결과 → 파일 적용용
        public PromptDisplayViewModel(string aiGitignoreText, Action<string> onApplyGitignore)
        {
            _promptText = aiGitignoreText;
            _onApplyGitignore = onApplyGitignore;
            IsGenerateButtonVisible = false;
            IsManualInputVisible = false;
            ApplyButtonText = "✅ .gitignore에 적용";
            Title = "AI가 생성한 .gitignore 내용";
            IsApplyButtonVisible = !string.IsNullOrWhiteSpace(aiGitignoreText);
        }


        [RelayCommand]
        private void CopyToClipboard()
        {
            try
            {
                Clipboard.SetText(PromptText);
                CopyButtonText = "✅ 복사 완료!";
            }
            catch (Exception ex)
            {
                CopyButtonText = "❌ 복사 실패";
                MessageBox.Show($"클립보드에 복사하는 중 오류가 발생했습니다.\n\n오류: {ex.Message}",
                    "복사 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(IsGenerateButtonVisible))]
        private void GenerateAiPrompt()
        {
            if (_promptService == null) return;
            string finalPrompt = _promptService.CreateCommitPrompt(PromptText);
            PromptText = finalPrompt;
            IsGenerateButtonVisible = false;
            Title = "생성된 AI 질문지 (복사하여 사용)";
        }

        [RelayCommand(CanExecute = nameof(IsApplyButtonVisible))]
        private void ApplyToCommit()
        {
            if (_onApplyCommit != null)
            {
                _onApplyCommit.Invoke(ParsedTitle, ParsedBody);
                RequestClose?.Invoke();
                return;
            }

            if (_onApplyGitignore != null)
            {
                _onApplyGitignore.Invoke(PromptText);
                RequestClose?.Invoke();
                return;
            }
        }

        [RelayCommand]
        private void ApplyManualInput()
        {
            if (_onApplyCommit == null) return;

            if (string.IsNullOrWhiteSpace(ManualTitle))
            {
                MessageBox.Show("제목을 입력해주세요.", "입력 필요",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ManualTitle.Length > 50)
            {
                var result = MessageBox.Show(
                    $"제목이 {ManualTitle.Length}자로 50자를 초과합니다.\n그래도 적용하시겠습니까?",
                    "제목 길이 경고", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            _onApplyCommit.Invoke(ManualTitle.Trim(), ManualBody.Trim());
            RequestClose?.Invoke();
        }


        // ▼▼▼ [핵심 수정] XML 태그 파싱으로 완전 교체 ▼▼▼
        private void ParseCommitMessage(string aiText)
        {
            try
            {
                // 1순위: <title>...</title> + <body>...</body> XML 태그 파싱
                var titleMatch = Regex.Match(aiText, @"<title>([\s\S]*?)</title>");
                var bodyMatch = Regex.Match(aiText, @"<body>([\s\S]*?)</body>");

                if (titleMatch.Success)
                {
                    ParsedTitle = titleMatch.Groups[1].Value.Trim();
                    ParsedBody = bodyMatch.Success
                        ? bodyMatch.Groups[1].Value.Trim()
                        : string.Empty;

                    Title = ParsedTitle.Length > 50
                        ? $"AI 커밋 메시지 ⚠️ 제목 {ParsedTitle.Length}자 (50자 초과!)"
                        : $"AI 커밋 메시지 ✅ 제목 {ParsedTitle.Length}자";

                    IsApplyButtonVisible = true;
                    IsManualInputVisible = false;
                    Console.WriteLine($"[DEBUG] XML 파싱 성공. 제목: {ParsedTitle}");
                    return;
                }

                // 2순위: 기존 git commit -m "..." 형식 (이전 버전 호환)
                var commitMatch = Regex.Match(aiText,
                    @"git commit -m ""([\s\S]+?)""", RegexOptions.Multiline);

                if (commitMatch.Success)
                {
                    string fullMessage = commitMatch.Groups[1].Value.Trim();
                    int firstNewline = fullMessage.IndexOf('\n');

                    ParsedTitle = firstNewline > 0
                        ? fullMessage[..firstNewline].Trim()
                        : fullMessage.Trim();
                    ParsedBody = firstNewline > 0
                        ? fullMessage[(firstNewline + 1)..].Trim()
                        : string.Empty;

                    Title = ParsedTitle.Length > 50
                        ? $"AI 커밋 메시지 ⚠️ 제목 {ParsedTitle.Length}자 (50자 초과!)"
                        : $"AI 커밋 메시지 ✅ 제목 {ParsedTitle.Length}자";

                    IsApplyButtonVisible = true;
                    IsManualInputVisible = false;
                    ParseWarningText = "⚠️ 구형 형식으로 파싱되었습니다. 내용을 확인 후 적용하세요.";
                    Console.WriteLine($"[DEBUG] git commit -m 형식으로 파싱 성공.");
                    return;
                }

                // 3순위: Conventional Commits 첫 줄 감지
                var lines = aiText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim().Trim('"').Trim('`');
                    if (Regex.IsMatch(trimmed,
                        @"^(feat|fix|refactor|docs|style|test|chore|perf|ci|build)\s*(\(.+\))?\s*:"))
                    {
                        int idx = Array.IndexOf(lines, line);
                        ParsedTitle = trimmed.Length > 50 ? trimmed[..50] : trimmed;
                        ParsedBody = idx + 1 < lines.Length
                            ? string.Join("\n", lines[(idx + 1)..]).Trim()
                            : string.Empty;

                        Title = ParsedTitle.Length > 50
                            ? $"AI 커밋 메시지 ⚠️ 제목 {ParsedTitle.Length}자 (50자 초과!)"
                            : $"AI 커밋 메시지 ✅ 제목 {ParsedTitle.Length}자";

                        IsApplyButtonVisible = true;
                        IsManualInputVisible = false;
                        ParseWarningText = "⚠️ AI 응답 형식이 달라 자동 파싱했습니다. 내용을 확인 후 적용하세요.";
                        Console.WriteLine($"[DEBUG] Conventional Commits 형식으로 파싱 성공.");
                        return;
                    }
                }

                // 최후: 모두 실패 → 수동 입력 UI
                Console.WriteLine($"[DEBUG] 모든 파싱 실패. 수동 입력 UI 표시.");
                ShowManualInputFallback(aiText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] 파싱 중 예외: {ex.Message}");
                ShowManualInputFallback(aiText);
            }
        }
        // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲

        private void ShowManualInputFallback(string aiText)
        {
            IsApplyButtonVisible = false;
            IsManualInputVisible = true;
            Title = "AI 커밋 메시지 ⚠️ 형식 파싱 실패 — 아래에 직접 입력하세요";
            ParseWarningText =
                "⚠️ AI 응답에서 커밋 메시지 형식을 찾지 못했습니다.\n" +
                "위 내용을 참고하여 아래 입력창에 제목과 본문을 직접 입력하세요.";

            var firstMeaningfulLine = aiText
                .Split('\n')
                .Select(l => l.Trim().Trim('"').Trim('`'))
                .FirstOrDefault(l => l.Length > 5 && !l.StartsWith("#") && !l.StartsWith("-"));

            if (!string.IsNullOrEmpty(firstMeaningfulLine))
                ManualTitle = firstMeaningfulLine.Length > 50
                    ? firstMeaningfulLine[..50]
                    : firstMeaningfulLine;
        }
    }
}
