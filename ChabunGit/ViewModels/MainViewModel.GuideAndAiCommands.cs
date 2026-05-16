// File: ChabunGit/ViewModels/MainViewModel.GuideAndAiCommands.cs
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ChabunGit.ViewModels
{
    public partial class MainViewModel
    {
        [RelayCommand]
        private async Task SelectFolderAsync()
        {
            var folderPath = _dialogService.ShowFolderBrowserDialog("프로젝트 폴더를 선택하세요.");
            if (!string.IsNullOrEmpty(folderPath))
                await SelectProjectFolder(folderPath);
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task GenerateGitignorePromptAsync()
        {
            IsBusy = true;
            BusyStatusText = "질문지 생성 중...";
            try
            {
                AddLog(".gitignore 생성을 위한 AI 질문지를 만드는 중...");

                IsBusy = false;
                var excludedPaths = _dialogService.ShowFolderSelector(SelectedFolder!);
                IsBusy = true;

                string prompt = await _promptService.CreateGitignorePromptAsync(
                    SelectedFolder!, excludedPaths ?? new List<string>());

                AddLog("AI 질문지 생성 완료.");
                _dialogService.ShowPrompt(".gitignore 질문지 생성", prompt);
            }
            catch (Exception ex)
            {
                AddLog($"❌ .gitignore 질문지 생성 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task GenerateGitignoreWithAIAsync()
        {
            IsBusy = true;
            BusyStatusText = "AI .gitignore 생성 중...";
            try
            {
                IsBusy = false;
                var excludedPaths = _dialogService.ShowFolderSelector(SelectedFolder!);
                IsBusy = true;

                AddLog("AI를 사용하여 .gitignore 내용을 생성하는 중...");
                if (excludedPaths != null && excludedPaths.Any())
                    AddLog($"사용자 지정 제외 항목 {excludedPaths.Count}개 포함.");

                // ▼▼▼ [추가] Ollama 내부 로그를 앱 로그창에 연결 ▼▼▼
                _promptService.OnLog = msg => AddLog(msg);
                // ▲▲▲ [추가] 여기까지 ▲▲▲

                string aiGeneratedContent = await _promptService.GenerateGitignoreContentAsync(
                    SelectedFolder!, excludedPaths ?? new List<string>());

                // ▼▼▼ [디버그] 응답 길이 확인 ▼▼▼
                AddLog($"🔍 AI 응답 길이: {aiGeneratedContent?.Length ?? 0}자");
                if (!string.IsNullOrWhiteSpace(aiGeneratedContent))
                    AddLog($"🔍 앞 50자: {aiGeneratedContent[..Math.Min(50, aiGeneratedContent.Length)]}");
                else
                    AddLog("⚠️ AI 응답이 비어있습니다.");
                // ▲▲▲ [디버그] 여기까지 ▲▲▲

                if (aiGeneratedContent.StartsWith("Ollama API 호출 중 오류"))
                {
                    AddLog($"❌ AI 생성 실패: {aiGeneratedContent}");
                    _dialogService.ShowMessage(
                        $"AI .gitignore 생성에 실패했습니다.\n\n{aiGeneratedContent}", "AI 오류");
                    return;
                }

                if (aiGeneratedContent == "Ollama 응답에서 메시지를 찾을 수 없습니다.")
                {
                    AddLog("❌ AI 생성 실패: Ollama 응답을 파싱하지 못했습니다. 로그를 확인하세요.");
                    _dialogService.ShowMessage(
                        "Ollama가 응답했지만 내용을 읽지 못했습니다.\n\n로그창의 [Ollama 원문] 내용을 확인해주세요.",
                        "파싱 오류");
                    return;
                }

                AddLog("AI .gitignore 생성 완료. 결과 창을 확인하세요.");

                _dialogService.ShowAiGitignoreResult(aiGeneratedContent, async (content) =>
                {
                    try
                    {
                        string gitignorePath = Path.Combine(SelectedFolder!, ".gitignore");
                        await File.WriteAllTextAsync(gitignorePath, content);
                        AddLog("✅ AI가 생성한 .gitignore 파일이 저장되었습니다.");
                        await RefreshRepositoryInfoAsync();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"❌ .gitignore 저장 중 오류: {ex.Message}");
                        _dialogService.ShowMessage(
                            $".gitignore 저장 중 오류:\n{ex.Message}", "저장 오류");
                    }
                });
            }
            catch (Exception ex)
            {
                AddLog($"❌ AI .gitignore 생성 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task AnalyzeChangesAsync()
        {
            IsBusy = true;
            BusyStatusText = "변경점 분석 중...";
            try
            {
                AddLog("변경점 분석을 시작합니다...");
                string diffContent = await _promptService.GetDiffAsync(SelectedFolder!);

                if (diffContent.Contains("변경 사항이 없습니다"))
                {
                    AddLog("⚠️ 변경 사항이 없습니다.");
                    _dialogService.ShowMessage("분석할 변경 사항이 없습니다.", "알림");
                    return;
                }

                _dialogService.ShowPrompt("AI 커밋 메시지 생성", diffContent, isForCommitAi: true);
                AddLog("변경점 분석 완료. AI 커밋 메시지 생성 창이 열렸습니다.");
            }
            catch (Exception ex)
            {
                AddLog($"❌ 변경점 분석 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task GenerateCommitMessageWithAIAsync()
        {
            IsBusy = true;
            BusyStatusText = "AI 커밋 메시지 생성 준비 중...";
            try
            {
                AddLog("AI를 사용하여 커밋 메시지를 생성하는 중...");

                var progress = new Progress<string>(message =>
                {
                    BusyStatusText = message;
                    AddLog(message);
                });

                string aiGeneratedMessage = await _promptService.GenerateCommitMessageAsync(SelectedFolder!, progress);

                if (aiGeneratedMessage.StartsWith("Ollama API 호출 중 오류") ||
                    aiGeneratedMessage.Contains("변경 사항이 없어"))
                {
                    AddLog($"❌ AI 생성 실패: {aiGeneratedMessage}");
                    _dialogService.ShowMessage(
                        $"AI 커밋 메시지 생성에 실패했습니다.\n\n{aiGeneratedMessage}", "AI 오류");
                    return;
                }

                AddLog("AI 커밋 메시지 생성 완료. 결과 창을 확인하세요.");

                _dialogService.ShowAiCommitResult(aiGeneratedMessage, (title, body) =>
                {
                    CommitTitle = title;
                    CommitBody = body;
                    AddLog($"✅ AI 커밋 메시지 적용 완료. 제목: {title}");
                });
            }
            catch (Exception ex)
            {
                AddLog($"❌ AI 커밋 메시지 생성 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task GenerateInitialCommitWithAIAsync()
        {
            IsBusy = true;
            BusyStatusText = "AI Initial Commit 생성 준비 중...";
            try
            {
                AddLog("AI를 사용하여 Initial Commit 메시지를 생성하는 중...");

                var progress = new Progress<string>(message =>
                {
                    BusyStatusText = message;
                    AddLog(message);
                });

                var startTime = DateTime.Now;

                string aiGeneratedMessage = await _promptService.GenerateInitialCommitMessageAsync(SelectedFolder!, progress);

                var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                AddLog($"AI Initial Commit 응답 수신 완료. 소요 시간: {elapsedSeconds:F1}초");

                if (aiGeneratedMessage.StartsWith("Ollama API 호출 중 오류") ||
                    aiGeneratedMessage.Contains("생성할 수 없습니다"))
                {
                    AddLog($"❌ AI 생성 실패: {aiGeneratedMessage}");
                    _dialogService.ShowMessage(
                        $"AI Initial Commit 메시지 생성에 실패했습니다.\n\n{aiGeneratedMessage}", "AI 오류");
                    return;
                }

                AddLog("AI Initial Commit 메시지 생성 완료. 결과 창을 확인하세요.");

                _dialogService.ShowAiCommitResult(aiGeneratedMessage, (title, body) =>
                {
                    CommitTitle = title;
                    CommitBody = body;
                    AddLog($"✅ AI Initial Commit 메시지 적용 완료. 제목: {title}");
                });
            }
            catch (Exception ex)
            {
                AddLog($"❌ AI Initial Commit 메시지 생성 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        [RelayCommand(CanExecute = nameof(GuideCanInit))]
        private async Task InitializeGitAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            BusyStatusText = "Git 저장소 초기화 중...";
            try
            {
                AddLog("Git 저장소 초기화 중...");
                var result = await _gitService.InitRepositoryAsync(SelectedFolder);
                AddLog(result.Output + result.Error);

                if (result.ExitCode == 0)
                {
                    await _gitService.EnsureUtf8ConfigAsync(SelectedFolder);
                    AddLog("✅ Git 저장소 초기화 성공!");

                    IsRepoValid = true;
                    GuideCanInit = false;
                    GuideCanAddRemote = true;
                }
                else
                {
                    _dialogService.ShowMessage($"Git 초기화 실패:\n{result.Error}", "오류");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Git 초기화 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        private bool CanAddRemote()
        {
            bool hasUrl = !string.IsNullOrWhiteSpace(NewProjectGitHubUrl);
            return hasUrl && (GuideCanAddRemote || IsLocalRepoWithoutRemote);
        }

        [RelayCommand(CanExecute = nameof(CanAddRemote))]
        private async Task AddRemoteAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            BusyStatusText = "원격 저장소 연결 중...";
            try
            {
                AddLog("원격 저장소 연결 중...");
                var result = await _gitService.AddRemoteAsync(SelectedFolder, NewProjectGitHubUrl.Trim());
                AddLog(result.Output + result.Error);

                if (result.ExitCode == 0)
                {
                    _dialogService.ShowMessage("원격 저장소가 성공적으로 연결되었습니다.", "성공");

                    NewProjectGitHubUrl = "";
                    IsLocalRepoWithoutRemote = false;
                    GuideCanAddRemote = false;
                    GuideCanComplete = true;
                }
                else
                {
                    _dialogService.ShowMessage($"원격 저장소 연결 실패:\n{result.Error}", "오류");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 원격 저장소 연결 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
                BusyStatusText = "처리 중...";
            }
        }

        [RelayCommand(CanExecute = nameof(GuideCanComplete))]
        private async Task CompleteGuideAsync()
        {
            if (SelectedFolder is null) return;
            IsBusy = true;
            try
            {
                AddLog("주 브랜치를 'main'으로 설정 중...");
                await _gitService.SetMainBranchAsync(SelectedFolder);

                IsNewProjectGuideActive = false;
                await RefreshRepositoryInfoAsync();
            }
            catch (Exception ex)
            {
                AddLog($"❌ 가이드 완료 중 오류: {ex.Message}");
                _dialogService.ShowMessage($"오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCopyLog() => !string.IsNullOrWhiteSpace(LogText);

        [RelayCommand(CanExecute = nameof(CanCopyLog))]
        private void CopyLog()
        {
            Clipboard.SetText(LogText);
            AddLog("✅ 로그 내용이 클립보드에 복사되었습니다.");
        }
    }
}
