// File: ChabunGit/ViewModels/MainViewModel.FileCommands.cs
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Threading.Tasks;

namespace ChabunGit.ViewModels
{
    /// <summary>
    /// 파일(.gitignore) 및 파일 목록(추적 중단)과 관련된 커맨드를 포함하는 partial 클래스입니다.
    /// </summary>
    public partial class MainViewModel
    {
        [RelayCommand(CanExecute = nameof(IsRepoValid))]
        private async Task EditGitignoreAsync()
        {
            string gitignorePath = Path.Combine(SelectedFolder!, ".gitignore");
            string content = File.Exists(gitignorePath) ? await File.ReadAllTextAsync(gitignorePath) : "# .NET 프로젝트 무시 파일 예시\n[Bb]in/\n[Oo]bj/";
            string? newContent = _dialogService.ShowGitignoreEditor(content);
            if (newContent != null)
            {
                await File.WriteAllTextAsync(gitignorePath, newContent);
                AddLog(".gitignore 파일이 수정되었습니다. 변경 파일 목록을 갱신합니다.");
                await RefreshRepositoryInfoAsync();
            }
        }

        private bool CanStopTrackingFile() => IsRepoValid && !string.IsNullOrEmpty(SelectedChangedFile);

        [RelayCommand(CanExecute = nameof(CanStopTrackingFile))]
        private async Task StopTrackingFileAsync(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || SelectedFolder is null) return;

            // 'git status --porcelain' 결과는 ' M 경로/파일명' 형식이므로 앞 3글자를 잘라내 순수 파일 경로를 얻습니다.
            string cleanFilePath = filePath.Substring(3);

            // 경로에 따옴표가 포함된 경우(공백 포함 파일명) 제거합니다.
            if (cleanFilePath.StartsWith("\"") && cleanFilePath.EndsWith("\""))
            {
                cleanFilePath = cleanFilePath[1..^1];
            }

            if (!_dialogService.ShowConfirmation($"'{cleanFilePath}' 파일의 추적을 중단하고 .gitignore에 추가하시겠습니까?\n(로컬 파일은 삭제되지 않습니다.)", "추적 중단 확인"))
            {
                return;
            }

            IsBusy = true;
            AddLog($"'{cleanFilePath}' 파일 추적 중단 중...");

            // 1. git rm --cached 실행
            var stopTrackingResult = await _gitService.StopTrackingFileAsync(SelectedFolder, cleanFilePath);
            AddLog(stopTrackingResult.Output + stopTrackingResult.Error);

            if (stopTrackingResult.ExitCode == 0)
            {
                AddLog($"✅ '{cleanFilePath}' 파일의 추적을 중단했습니다.");

                // 2. .gitignore 파일에 경로 추가
                try
                {
                    string gitignorePath = Path.Combine(SelectedFolder, ".gitignore");
                    // .gitignore 표준에 맞게 역슬래시를 슬래시로 변환하고, 파일 맨 끝에 추가하기 전 줄바꿈을 추가합니다.
                    string ignoreEntry = "\n" + cleanFilePath.Replace("\\", "/");
                    await File.AppendAllTextAsync(gitignorePath, ignoreEntry);
                    AddLog($"'{cleanFilePath}' 항목을 .gitignore에 추가했습니다.");
                }
                catch (System.Exception ex)
                {
                    AddLog($"❌ .gitignore 파일에 쓰는 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                AddLog($"❌ 파일 추적 중단 실패.");
            }

            // 3. UI 새로고침
            await RefreshRepositoryInfoAsync();
            IsBusy = false;
        }
    }
}