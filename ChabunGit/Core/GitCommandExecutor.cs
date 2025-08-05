// 파일 경로: ChabunGit/Core/GitCommandExecutor.cs (수정된 코드)

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Core
{
    /// <summary>
    /// git.exe 프로세스를 실행하고 결과를 반환하는 역할을 전담하는 클래스입니다.
    /// </summary>
    public class GitCommandExecutor
    {
        /// <summary>
        /// Git 명령어 실행 결과를 담는 레코드(Record)입니다. 불변성을 가집니다.
        /// </summary>
        /// <param name="Output">표준 출력 결과</param>
        /// <param name="Error">표준 오류 결과</param>
        /// <param name="ExitCode">프로세스 종료 코드 (0이면 성공)</param>
        public record ProcessResult(string Output, string Error, int ExitCode);

        /// <summary>
        /// 지정된 경로에서 Git 명령어를 비동기적으로 실행합니다.
        /// </summary>
        /// <param name="workingDirectory">명령어를 실행할 Git 저장소 경로</param>
        /// <param name="arguments">git 명령어 뒤에 붙을 인자 (예: "status", "commit -m \"message\"")</param>
        /// <returns>명령어 실행 결과를 담은 ProcessResult 객체</returns>
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

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            return new ProcessResult(output, error, process.ExitCode);
        }
    }
}