// File: ChabunGit/Core/GitCommandExecutor.cs
using System.Collections.Concurrent; // 추가
using System.Diagnostics;
using System.Text;
using System.Threading; // 추가
using System.Threading.Tasks;

namespace ChabunGit.Core
{
    public class GitCommandExecutor
    {
        // ▼▼▼ [수정] 실행될 명령어를 전달하기 위한 이벤트를 추가합니다. ▼▼▼
        public event System.Action<string>? OnCommandExecuting;

        public record ProcessResult(string Output, string Error, int ExitCode);

        // 저장소 경로별로 SemaphoreSlim을 관리하는 스레드 안전한 딕셔너리
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _repositoryLocks = new();

        public async Task<ProcessResult> ExecuteAsync(string workingDirectory, string arguments)
        {
            // ▼▼▼ [수정] 명령어를 실행하기 직전에 이벤트를 호출하여 로그를 남길 수 있도록 합니다. ▼▼▼
            OnCommandExecuting?.Invoke($"git {arguments}");

            // 현재 작업 디렉토리에 해당하는 SemaphoreSlim을 가져오거나 생성 (초기 카운트 1)
            var gate = _repositoryLocks.GetOrAdd(workingDirectory, _ => new SemaphoreSlim(1, 1));

            // 한 번에 하나의 스레드만 진입하도록 대기
            await gate.WaitAsync();
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                using var process = new Process { StartInfo = processStartInfo };

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                return new ProcessResult(output, error, process.ExitCode);
            }
            finally
            {
                // 작업이 끝나면 반드시 SemaphoreSlim을 해제하여 다음 대기 스레드가 진입할 수 있도록 함
                gate.Release();
            }
        }
    }
}
