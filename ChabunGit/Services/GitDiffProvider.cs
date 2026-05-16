using ChabunGit.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class GitDiffProvider
    {
        private readonly GitCommandExecutor _executor;
        private const int MaxDiffLength = 8000;
        private const int MaxPerFileDiff = 3000;

        public GitDiffProvider(GitCommandExecutor executor)
        {
            _executor = executor;
        }

        public async Task<string> GetDiffAsync(string repoPath)
        {
            try
            {
                var fileListResult = await _executor.ExecuteAsync(repoPath, "diff --cached --name-only");
                if (string.IsNullOrWhiteSpace(fileListResult.Output))
                    fileListResult = await _executor.ExecuteAsync(repoPath, "diff --name-only");

                var changedFiles = fileListResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (changedFiles.Count == 0)
                {
                    var headFileList = await _executor.ExecuteAsync(repoPath, "diff --name-only HEAD");
                    changedFiles = headFileList.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (changedFiles.Count == 0) return "커밋할 변경 사항이 없습니다.";
                }

                var resultBuilder = new StringBuilder();
                int totalLength = 0;
                foreach (var file in changedFiles)
                {
                    if (totalLength >= MaxDiffLength) break;
                    var fileDiffResult = await _executor.ExecuteAsync(repoPath, $"diff --cached --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                        fileDiffResult = await _executor.ExecuteAsync(repoPath, $"diff --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(fileDiffResult.Output))
                        fileDiffResult = await _executor.ExecuteAsync(repoPath, $"diff --text HEAD -- \"{file}\"");

                    if (!string.IsNullOrWhiteSpace(fileDiffResult.Output))
                    {
                        string fileDiff = fileDiffResult.Output;
                        if (fileDiff.Length > (MaxDiffLength - totalLength))
                            fileDiff = fileDiff[..(MaxDiffLength - totalLength)] + "\n... [diff 잘림]";
                        resultBuilder.AppendLine(fileDiff);
                        totalLength += fileDiff.Length;
                    }
                }
                string finalDiff = resultBuilder.ToString().Trim();
                return string.IsNullOrWhiteSpace(finalDiff) ? "커밋할 변경 사항이 없습니다." : finalDiff;
            }
            catch (Exception ex)
            {
                return $"diff 가져오기 중 오류 발생: {ex.Message}";
            }
        }

        public async Task<List<(string FilePath, string Diff)>> GetDiffPerFileAsync(string repoPath)
        {
            var result = new List<(string, string)>();
            try
            {
                var fileListResult = await _executor.ExecuteAsync(repoPath, "diff --cached --name-only");
                if (string.IsNullOrWhiteSpace(fileListResult.Output))
                    fileListResult = await _executor.ExecuteAsync(repoPath, "diff --name-only");

                var changedFiles = fileListResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (changedFiles.Count == 0)
                {
                    var headFileList = await _executor.ExecuteAsync(repoPath, "diff --name-only HEAD");
                    changedFiles = headFileList.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                foreach (var file in changedFiles)
                {
                    var diffResult = await _executor.ExecuteAsync(repoPath, $"diff --cached --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(diffResult.Output))
                        diffResult = await _executor.ExecuteAsync(repoPath, $"diff --text -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(diffResult.Output))
                        diffResult = await _executor.ExecuteAsync(repoPath, $"diff --text HEAD -- \"{file}\"");
                    if (string.IsNullOrWhiteSpace(diffResult.Output)) continue;

                    string diff = diffResult.Output;
                    if (diff.Length > MaxPerFileDiff)
                        diff = diff[..MaxPerFileDiff] + "\n... [이 파일의 diff가 길어 일부만 포함됨]";
                    result.Add((file, diff));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetDiffPerFileAsync: {ex.Message}");
            }
            return result;
        }
    }
}