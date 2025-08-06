// File: ChabunGit/Core/GitCommandExecutor.cs
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Core
{
    public class GitCommandExecutor
    {
        // ▼▼▼ [수정] 실행될 명령어를 전달하기 위한 이벤트를 추가합니다. ▼▼▼
        public event System.Action<string>? OnCommandExecuting;

        public record ProcessResult(string Output, string Error, int ExitCode);

        public async Task<ProcessResult> ExecuteAsync(string workingDirectory, string arguments)
        {
            // ▼▼▼ [수정] 명령어를 실행하기 직전에 이벤트를 호출하여 로그를 남길 수 있도록 합니다. ▼▼▼
            OnCommandExecuting?.Invoke($"git {arguments}");

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
    }
}