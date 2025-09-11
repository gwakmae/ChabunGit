// File: ChabunGit/Services/GitService.cs
using ChabunGit.Core;
using ChabunGit.Models;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ChabunGit.Services
{
    public class GitService : IGitService
    {
        public GitCommandExecutor Executor { get; }

        public GitService(GitCommandExecutor executor)
        {
            Executor = executor;
        }

        public bool IsGitRepository(string path) => Directory.Exists(Path.Combine(path, ".git"));
        public async Task<GitCommandExecutor.ProcessResult> InitRepositoryAsync(string path) => await Executor.ExecuteAsync(path, "init");
        public async Task<GitCommandExecutor.ProcessResult> AddRemoteAsync(string repoPath, string remoteUrl) => await Executor.ExecuteAsync(repoPath, $"remote add origin {remoteUrl}");
        public async Task<GitCommandExecutor.ProcessResult> SetMainBranchAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "branch -M main");
        public async Task<GitCommandExecutor.ProcessResult> GetChangedFilesAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "status --porcelain");

        public async Task<List<CommitInfo>> GetCommitHistoryAsync(string repoPath, int maxCount = 50)
        {
            string format = "--pretty=format:\"%H|%h|%ad|%s\"";
            string dateformat = "--date=short";
            var result = await Executor.ExecuteAsync(repoPath, $"log -{maxCount} {format} {dateformat}");

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output)) return new List<CommitInfo>();

            return result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('|'))
                .Where(parts => parts.Length == 4)
                .Select(parts => new CommitInfo
                {
                    Hash = parts[0].Trim(),
                    ShortHash = parts[1].Trim(),
                    Date = parts[2].Trim(),
                    Message = parts[3].Trim()
                })
                .ToList();
        }

        public async Task<GitCommandExecutor.ProcessResult> StageAllChangesAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "add .");

        public async Task<GitCommandExecutor.ProcessResult> CommitAsync(string repoPath, string title, string body)
        {
            // 1. 제목과 본문을 하나의 문자열로 합칩니다.
            //    제목과 본문 사이에는 두 번의 줄바꿈(공백 라인)을 넣어 문단을 구분합니다.
            string fullMessage = $"{title.Trim()}\n\n{body.Trim()}";

            // 2. 이스케이프 처리가 필요한 문자를 처리합니다. (특히 따옴표)
            string escapedMessage = fullMessage.Replace("\"", "\\\"");

            // 3. 단 하나의 -m 플래그를 사용하여 전체 메시지를 전달합니다.
            string command = $"commit -m \"{escapedMessage}\"";

            return await Executor.ExecuteAsync(repoPath, command);
        }

        public async Task<GitCommandExecutor.ProcessResult> FetchAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "fetch");
        public async Task<GitCommandExecutor.ProcessResult> PullAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "pull");

        public async Task<GitCommandExecutor.ProcessResult> PushAsync(string repoPath, bool isForce, bool isFirstPush)
        {
            string command = "push";
            if (isFirstPush) command += " -u origin HEAD";
            if (isForce) command += " --force";
            return await Executor.ExecuteAsync(repoPath, command);
        }

        public async Task<GitCommandExecutor.ProcessResult> ResetLastCommitAsync(string repoPath) => await Executor.ExecuteAsync(repoPath, "reset --soft HEAD~1");
        public async Task<GitCommandExecutor.ProcessResult> ResetToCommitAsync(string repoPath, string commitHash) => await Executor.ExecuteAsync(repoPath, $"reset --hard {commitHash}");

        public async Task<GitCommandExecutor.ProcessResult> GetCommitDetailsAsync(string repoPath, string commitHash)
        {
            return await Executor.ExecuteAsync(repoPath, $"show --stat --pretty=fuller {commitHash}");
        }

        public async Task<bool> HasRemoteAsync(string repoPath)
        {
            // "git remote" 명령어는 설정된 원격 저장소가 있으면 그 이름을 출력하고, 없으면 아무것도 출력하지 않습니다.
            var result = await Executor.ExecuteAsync(repoPath, "remote");

            // 결과의 ExitCode가 0이고, 표준 출력이 비어있지 않다면 원격 저장소가 존재하는 것입니다.
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output);
        }

        public async Task EnsureUtf8ConfigAsync(string repoPath)
        {
            try
            {
                string configPath = Path.Combine(repoPath, ".git", "config");

                // .git/config 파일이 존재하는지 확인
                if (!File.Exists(configPath))
                {
                    return; // 파일이 없으면 아무것도 하지 않음
                }

                string content = await File.ReadAllTextAsync(configPath, Encoding.UTF8);

                // 이미 설정이 있는지 간단하게 확인
                if (content.Contains("logoutputencoding") || content.Contains("[i18n]"))
                {
                    return; // 설정이 이미 있으면 아무것도 하지 않음
                }

                // 추가할 설정 텍스트 생성
                var settingBuilder = new StringBuilder();
                settingBuilder.AppendLine(); // 기존 내용과 한 줄 띄움
                settingBuilder.AppendLine("[i18n]");
                settingBuilder.AppendLine("\tcommitEncoding = utf-8");
                settingBuilder.AppendLine("\tlogOutputEncoding = utf-8");

                // 파일 끝에 설정 추가
                await File.AppendAllTextAsync(configPath, settingBuilder.ToString(), Encoding.UTF8);
            }
            catch (Exception)
            {
                // 오류가 발생하더라도 프로그램이 멈추지 않도록 처리
                // (예: 권한 문제 등)
            }
        }

        // ▼▼▼ [추가] 파일 추적을 중단하는 git rm --cached 명령어를 실행하는 메서드를 추가합니다. ▼▼▼
        public async Task<GitCommandExecutor.ProcessResult> StopTrackingFileAsync(string repoPath, string filePath)
        {
            // 파일 경로에 공백이 있을 수 있으므로 따옴표로 감싸줍니다.
            return await Executor.ExecuteAsync(repoPath, $"rm --cached \"{filePath}\"");
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲

        // ▼▼▼ [추가] 인덱스 잠금 파일(index.lock) 삭제를 시도하는 메서드를 추가합니다. ▼▼▼
        public Task<bool> TryUnlockIndexAsync(string repoPath)
        {
            try
            {
                string lockPath = Path.Combine(repoPath, ".git", "index.lock");
                if (File.Exists(lockPath))
                {
                    File.Delete(lockPath);
                }
                return Task.FromResult(true); // 파일이 없거나, 삭제에 성공하면 true
            }
            catch (Exception ex)
            {
                // 실제 운영 코드에서는 로깅을 하는 것이 좋습니다.
                Debug.WriteLine($"Failed to delete index.lock: {ex.Message}");
                return Task.FromResult(false); // 권한 등의 문제로 삭제 실패 시
            }
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲
    }
}
