// File: ChabunGit/Core/GitCommandExecutor.cs
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Core
{
    public class GitCommandExecutor
    {
        public record ProcessResult(string Output, string Error, int ExitCode);

        public async Task<ProcessResult> ExecuteAsync(string workingDirectory, string arguments)
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
    }
}