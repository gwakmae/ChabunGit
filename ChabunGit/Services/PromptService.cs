using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly OllamaClient _ollamaClient;
        private readonly PromptBuilder _promptBuilder;
        private readonly GitDiffProvider _diffProvider;

        public PromptService(GitCommandExecutor executor, HttpClient httpClient)
        {
            _ollamaClient = new OllamaClient(httpClient);
            _promptBuilder = new PromptBuilder();
            _diffProvider = new GitDiffProvider(executor);
        }

        public Task<string> CreateInitialCommitPromptAsync(string repoPath) => _promptBuilder.CreateInitialCommitPromptAsync(repoPath);
        public Task<string> CreateGitignorePromptAsync(string repoPath, List<string>? excludedPaths = null) => _promptBuilder.CreateGitignorePromptAsync(repoPath, excludedPaths);
        public string CreateCommitPrompt(string diffContent) => _promptBuilder.CreateCommitPrompt(diffContent);
        public Task<string> GetDiffAsync(string repoPath) => _diffProvider.GetDiffAsync(repoPath);

        public async Task<string> GenerateGitignoreContentAsync(string repoPath, List<string>? excludedPaths = null)
        {
            var prompt = await CreateGitignorePromptAsync(repoPath, excludedPaths);
            return await _ollamaClient.GenerateAsync(prompt);
        }

        public async Task<string> GenerateCommitMessageAsync(string repoPath, IProgress<string>? progress = null)
        {
            progress?.Report("🔍 변경된 파일 목록을 가져오는 중...");
            var perFileDiffs = await _diffProvider.GetDiffPerFileAsync(repoPath);
            if (perFileDiffs.Count == 0)
            {
                progress?.Report("⚠️ 변경 사항이 없습니다.");
                return "변경 사항이 없어 커밋 메시지를 생성할 수 없습니다.";
            }

            int totalFiles = perFileDiffs.Count;
            progress?.Report($"📂 총 {totalFiles}개 파일 변경 감지. 분석을 시작합니다.");

            var fileSummaries = new List<string>();
            var startTime = DateTime.Now;
            int processedCount = 0;

            foreach (var (filePath, diff) in perFileDiffs)
            {
                processedCount++;
                string etaText = "";
                if (processedCount > 1)
                {
                    var elapsed = DateTime.Now - startTime;
                    var avgPerFile = elapsed.TotalSeconds / (processedCount - 1);
                    var remainingFiles = totalFiles - processedCount + 1;
                    var etaSeconds = avgPerFile * remainingFiles;
                    etaText = etaSeconds > 60 ? $" (예상 남은 시간: 약 {etaSeconds / 60:F1}분)" : $" (예상 남은 시간: 약 {etaSeconds:F0}초)";
                }
                progress?.Report($"📝 [{processedCount}/{totalFiles}] 분석 중: {filePath}{etaText}");

                string mapPrompt = $@"You are analyzing a single file's diff. Output ONLY one short English line describing what changed.
RULES:
- Format: '<filename>: <what changed in one short sentence>'
- Max 100 characters.
- Imperative mood (Add, Fix, Update, Remove).
- No bullets, no markdown, no quotes, no explanation.
- ONLY one line.
FILE: {filePath}
DIFF:
{diff}";
                string rawSummary = await _ollamaClient.GenerateAsync(mapPrompt, maxTokens: 80);
                string summary = AiResponseCleaner.CleanSingleLine(rawSummary);
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    fileSummaries.Add($"- {summary}");
                    progress?.Report($"    ✅ {summary}");
                }
                else
                {
                    fileSummaries.Add($"- {filePath}: modified");
                    progress?.Report($"    ⚠️ 빈 응답 → 파일명만 기록: {filePath}");
                }
            }

            string accumulatedSummary = string.Join("\n", fileSummaries);
            var totalElapsed = (DateTime.Now - startTime).TotalSeconds;
            progress?.Report($"✅ Map 단계 완료. {totalFiles}개 파일 요약 ({totalElapsed:F1}초 소요)");

            progress?.Report("🎯 [Reduce 1/2] 누적 요약을 바탕으로 커밋 제목 생성 중...");
            string titlePrompt = $@"Below is a list of file changes in this commit. Generate ONLY the commit title.
RULES:
- Format: <type>: <description> (type = feat|fix|refactor|docs|style|test|chore|perf)
- Max 50 characters total.
- Imperative mood. No period. No quotes. No markdown. No explanation.
- Output ONLY one single line.
CHANGES:
{accumulatedSummary}";
            string rawTitle = await _ollamaClient.GenerateAsync(titlePrompt, maxTokens: 100);
            string title = AiResponseCleaner.CleanTitle(rawTitle);
            progress?.Report($"    ✅ 제목 생성: {title}");

            progress?.Report("📄 [Reduce 2/2] 누적 요약을 바탕으로 커밋 본문 생성 중...");
            string bodyPrompt = $@"Below is a list of file changes in this commit, plus the commit title.
Write the commit body that summarizes the changes.
TITLE: {title}
CHANGES:
{accumulatedSummary}
RULES:
- Write 3-5 bullet points in English starting with '-'.
- Then a blank line.
- Then write 3-5 bullet points in Korean starting with '-'.
- Group related changes together, do NOT just copy file names.
- No title repetition. No XML tags. No markdown headers. No code blocks.
- Output ONLY the bullets.";
            string rawBody = await _ollamaClient.GenerateAsync(bodyPrompt, maxTokens: 600);
            string body = AiResponseCleaner.CleanBody(rawBody);
            if (string.IsNullOrWhiteSpace(body))
            {
                progress?.Report("⚠️ 본문 생성 실패 → 누적 요약을 본문으로 대체");
                body = "Changes summary:\n" + accumulatedSummary;
            }
            else
            {
                progress?.Report($"    ✅ 본문 생성 완료 ({body.Length}자)");
            }

            progress?.Report($"🎉 전체 완료! 총 소요 시간: {(DateTime.Now - startTime).TotalSeconds:F1}초");
            return $"<title>{title}</title>\n<body>\n{body}\n</body>";
        }

        public async Task<string> GenerateInitialCommitMessageAsync(string repoPath, IProgress<string>? progress = null)
        {
            progress?.Report("📂 프로젝트 구조를 분석하는 중...");
            string projectContext = await CreateInitialCommitPromptAsync(repoPath);
            progress?.Report($"✅ 프로젝트 컨텍스트 수집 완료 ({projectContext.Length}자)");
            if (string.IsNullOrWhiteSpace(projectContext))
                return "Initial Commit 메시지를 생성할 수 없습니다. 분석할 프로젝트 정보가 없습니다.";

            progress?.Report("🎯 [1/2] Initial Commit 제목 생성 중...");
            string titlePrompt = $@"You are analyzing a project's first commit. Read the project structure below and output ONLY the commit title.
RULES:
- Format: feat: <short project description> OR init: <project name>
- Max 50 characters total.
- Imperative mood. No period at end. No quotes. No markdown. No explanation.
- Output ONLY one single line.
PROJECT CONTEXT:
{projectContext}";
            string rawTitle = await _ollamaClient.GenerateAsync(titlePrompt, maxTokens: 150);
            string title = AiResponseCleaner.CleanTitle(rawTitle);
            progress?.Report($"    ✅ 제목: {title}");

            progress?.Report("📄 [2/2] Initial Commit 본문 생성 중...");
            string bodyPrompt = $@"You are writing the body of a project's first commit message.
TITLE: {title}
RULES:
- 2-4 English bullets starting with '-' describing main features/structure.
- Then a blank line.
- 2-4 Korean bullets starting with '-'.
- No title repetition. No XML tags. No markdown headers. No code blocks.
- Output ONLY the bullets.
PROJECT CONTEXT:
{projectContext}";
            string rawBody = await _ollamaClient.GenerateAsync(bodyPrompt, maxTokens: 600);
            string body = AiResponseCleaner.CleanBody(rawBody);
            progress?.Report("🎉 Initial Commit 메시지 생성 완료!");
            return $"<title>{title}</title>\n<body>\n{body}\n</body>";
        }
    }
}