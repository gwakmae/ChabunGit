// File: ChabunGit/Services/PromptService.cs
using ChabunGit.Core;
using ChabunGit.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class PromptService : IPromptService
    {
        private readonly OllamaClient _ollamaClient;
        private readonly PromptBuilder _promptBuilder;
        private readonly GitDiffProvider _diffProvider;

        // ▼ [추가] 누적 요약이 너무 길면 컨텍스트를 초과해 빈 응답이 나올 수 있으므로 상한을 둡니다.
        private const int MaxAccumulatedSummaryChars = 4000;

        public Action<string>? OnLog { get; set; }

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

        // ========================================================================
        // 📦 .gitignore 생성 (토큰 확대 + 정제 + 폴백 적용)
        // ========================================================================
        public async Task<string> GenerateGitignoreContentAsync(string repoPath, List<string>? excludedPaths = null)
        {
            var prompt = await CreateGitignorePromptAsync(repoPath, excludedPaths);
            _ollamaClient.OnLog = OnLog;

            try
            {
                // 1. 토큰 제한을 2500으로 확대하여 잘림 현상 방지
                string result = await _ollamaClient.GenerateAsync(prompt, maxTokens: 2500);

                // 2. ▼ [수정] IsErrorResult로 오류/빈 응답을 한 번에 감지하여 폴백 ▼
                if (OllamaClient.IsErrorResult(result))
                {
                    OnLog?.Invoke("⚠️ AI 응답이 비어있거나 오류가 발생하여 기본 .gitignore 템플릿으로 대체합니다.");
                    return GenerateFallbackGitignore(excludedPaths);
                }

                // 3. AI가 붙인 코드블럭/서두 제거 후 반환
                return AiResponseCleaner.CleanGitignore(result);
            }
            finally
            {
                // 작업 후 모델을 VRAM에서 언로드
                await _ollamaClient.UnloadModelAsync();
            }
        }

        private string GenerateFallbackGitignore(List<string>? excludedPaths)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 📦 Build & Dependencies");
            sb.AppendLine("bin/\nobj/\nnode_modules/\npackages/\n__pycache__/\n*.pyc");
            sb.AppendLine("\n# 💻 IDE & Editor");
            sb.AppendLine(".vs/\n.vscode/\n.idea/\n*.suo\n*.user");
            sb.AppendLine("\n# 🌐 OS");
            sb.AppendLine(".DS_Store\nThumbs.db\ndesktop.ini");

            if (excludedPaths != null && excludedPaths.Any())
            {
                sb.AppendLine("\n# 🚫 User Excluded");
                foreach (var path in excludedPaths) sb.AppendLine(path);
            }
            return sb.ToString();
        }

        // ========================================================================
        // 🔄 일반 커밋 메시지 생성 (Map-Reduce + Frozen 캐시 + 센티넬 감지)
        // ========================================================================
        public async Task<string> GenerateCommitMessageAsync(string repoPath, IProgress<string>? progress = null)
        {
            // ▼ [수정] OllamaClient 로그를 항상 연결 (기존에는 gitignore 생성 시에만 연결됨) ▼
            _ollamaClient.OnLog = OnLog;

            progress?.Report("🔍 변경된 파일 목록을 가져오는 중...");
            var perFileDiffs = await _diffProvider.GetDiffPerFileAsync(repoPath);
            if (perFileDiffs.Count == 0)
            {
                progress?.Report("⚠️ 변경 사항이 없습니다.");
                return "변경 사항이 없어 커밋 메시지를 생성할 수 없습니다.";
            }

            // Frozen 캐시 로드
            var cache = new AiSummaryCache(repoPath);
            await cache.LoadAsync();
            int cacheHits = 0;

            int totalFiles = perFileDiffs.Count;
            progress?.Report($"📂 총 {totalFiles}개 파일 변경 감지. 분석을 시작합니다.");
            var fileSummaries = new List<string>();
            var startTime = DateTime.Now;
            int processedCount = 0;

            try
            {
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

                    // 캐시 조회: diff가 동일하면 GPU 호출 생략
                    string cacheKey = AiSummaryCache.ComputeKey(filePath, diff);
                    if (cache.TryGet(cacheKey, out string cachedSummary))
                    {
                        cacheHits++;
                        fileSummaries.Add($"- {cachedSummary}");
                        progress?.Report($"📦 [{processedCount}/{totalFiles}] 캐시 사용: {filePath}");
                        continue;
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

                    // ▼▼▼ [핵심 수정] 오류 센티넬을 먼저 걸러냅니다.
                    // 기존에는 "Ollama 응답에서..." 문자열이 CleanSingleLine을 통과해
                    // 요약인 것처럼 저장되고 캐시에까지 오염되어 남았습니다. ▼▼▼
                    if (OllamaClient.IsErrorResult(rawSummary))
                    {
                        fileSummaries.Add($"- {filePath}: modified");
                        progress?.Report($"    ⚠️ AI 응답 실패 → 파일명만 기록: {filePath}");
                        continue; // 실패한 결과는 캐시에 저장하지 않습니다.
                    }
                    // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲

                    string summary = AiResponseCleaner.CleanSingleLine(rawSummary);
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        fileSummaries.Add($"- {summary}");
                        // 성공한 분석 결과만 frozen 텍스트로 영구 저장
                        cache.Set(cacheKey, summary);
                        progress?.Report($"    ✅ {summary}");
                    }
                    else
                    {
                        fileSummaries.Add($"- {filePath}: modified");
                        progress?.Report($"    ⚠️ 빈 응답 → 파일명만 기록: {filePath}");
                    }
                }

                // 캐시를 디스크에 저장하고 결과 보고
                await cache.SaveAsync();
                if (cacheHits > 0)
                    progress?.Report($"📦 캐시 재사용 {cacheHits}/{totalFiles}개 파일 (GPU 호출 {cacheHits}회 절약)");

                string accumulatedSummary = string.Join("\n", fileSummaries);

                // ▼▼▼ [핵심 추가] 누적 요약 길이 상한.
                // 파일이 많으면 요약 합본이 컨텍스트 한계를 넘어
                // 제목/본문 생성이 빈 응답으로 실패하는 것을 방지합니다. ▼▼▼
                if (accumulatedSummary.Length > MaxAccumulatedSummaryChars)
                {
                    progress?.Report($"✂️ 누적 요약이 {accumulatedSummary.Length}자로 길어 {MaxAccumulatedSummaryChars}자로 축소합니다.");
                    accumulatedSummary = accumulatedSummary[..MaxAccumulatedSummaryChars]
                        + "\n... [요약이 길어 일부 파일 생략됨]";
                }
                // ▲▲▲ [핵심 추가] 여기까지 ▲▲▲

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

                // ▼▼▼ [수정] 오류 센티넬을 감지하여 폴백으로 보냅니다. ▼▼▼
                string rawTitle = await _ollamaClient.GenerateAsync(titlePrompt, maxTokens: 100);
                string title = OllamaClient.IsErrorResult(rawTitle)
                    ? string.Empty
                    : AiResponseCleaner.CleanTitle(rawTitle);

                if (string.IsNullOrWhiteSpace(title) || title == "chore: update project")
                {
                    title = BuildFallbackTitle(fileSummaries);
                    progress?.Report($"    ⚠️ 제목 생성 실패 → 요약 기반 기본 제목으로 대체: {title}");
                }
                else
                {
                    progress?.Report($"    ✅ 제목 생성: {title}");
                }
                // ▲▲▲ [수정] 여기까지 ▲▲▲

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

                // ▼▼▼ [핵심 수정] 오류 센티넬을 빈 문자열로 정규화한 뒤 정제합니다.
                // 기존에는 "Ollama 응답에서 메시지를 찾을 수 없습니다."가
                // 비어있지 않은 문자열이라 폴백 체크(IsNullOrWhiteSpace)를 통과해
                // <body> 안에 그대로 들어갔습니다. ▼▼▼
                string rawBody = await _ollamaClient.GenerateAsync(bodyPrompt, maxTokens: 600);
                string body = OllamaClient.IsErrorResult(rawBody)
                    ? string.Empty
                    : AiResponseCleaner.CleanBody(rawBody);

                if (string.IsNullOrWhiteSpace(body))
                {
                    progress?.Report("⚠️ 본문 생성 실패 → 누적 요약을 본문으로 대체");
                    body = "Changes summary:\n" + accumulatedSummary;
                }
                else
                {
                    progress?.Report($"    ✅ 본문 생성 완료 ({body.Length}자)");
                }
                // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲

                progress?.Report($"🎉 전체 완료! 총 소요 시간: {(DateTime.Now - startTime).TotalSeconds:F1}초");
                return $"<title>{title}</title>\n<body>\n{body}\n</body>";
            }
            finally
            {
                // 성공/실패와 무관하게 작업 후 모델을 VRAM에서 언로드
                await _ollamaClient.UnloadModelAsync();
            }
        }

        // ▼▼▼ [추가] 제목 생성 실패 시 파일 요약에서 키워드를 뽑아 기본 제목 생성 ▼▼▼
        private static string BuildFallbackTitle(List<string> fileSummaries)
        {
            string joined = string.Join(" ", fileSummaries).ToLowerInvariant();
            string type =
                joined.Contains("fix") ? "fix"
                : joined.Contains("add") ? "feat"
                : joined.Contains("refactor") ? "refactor"
                : joined.Contains("style") || joined.Contains("css") ? "style"
                : joined.Contains("remove") || joined.Contains("delete") ? "refactor"
                : "chore";
            return $"{type}: update {fileSummaries.Count} files";
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲

        // ========================================================================
        // 🚀 Initial Commit 메시지 생성 (토큰 확대 + 오류 감지 + 폴백 적용)
        // ========================================================================
        public async Task<string> GenerateInitialCommitMessageAsync(string repoPath, IProgress<string>? progress = null)
        {
            _ollamaClient.OnLog = OnLog;

            progress?.Report("📂 프로젝트 구조를 분석하는 중...");
            string projectContext = await CreateInitialCommitPromptAsync(repoPath);
            progress?.Report($"✅ 프로젝트 컨텍스트 수집 완료 ({projectContext.Length}자)");

            if (string.IsNullOrWhiteSpace(projectContext))
                return "Initial Commit 메시지를 생성할 수 없습니다. 분석할 프로젝트 정보가 없습니다.";

            try
            {
                progress?.Report("🎯 [1/2] Initial Commit 제목 생성 중...");
                string titlePrompt = $@"You are analyzing a project's first commit. Read the project structure below and output ONLY the commit title.
RULES:
- Format: feat: <short project description> OR init: <project name>
- Max 50 characters total.
- Imperative mood. No period at end. No quotes. No markdown. No explanation.
- Output ONLY one single line.
PROJECT CONTEXT:
{projectContext}";

                // ▼ [수정] IsErrorResult로 오류 응답을 감지하여 폴백 ▼
                string rawTitle = await _ollamaClient.GenerateAsync(titlePrompt, maxTokens: 300);
                string title = OllamaClient.IsErrorResult(rawTitle)
                    ? string.Empty
                    : AiResponseCleaner.CleanTitle(rawTitle);

                if (string.IsNullOrWhiteSpace(title) || title == "chore: update project")
                {
                    progress?.Report("⚠️ 제목 생성 실패 → 기본 제목으로 대체");
                    title = "init: project setup";
                }
                else
                {
                    progress?.Report($"    ✅ 제목: {title}");
                }

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

                // ▼ [수정] IsErrorResult로 오류 응답을 감지하여 폴백 ▼
                string rawBody = await _ollamaClient.GenerateAsync(bodyPrompt, maxTokens: 1500);
                string body = OllamaClient.IsErrorResult(rawBody)
                    ? string.Empty
                    : AiResponseCleaner.CleanBody(rawBody);

                if (string.IsNullOrWhiteSpace(body))
                {
                    progress?.Report("⚠️ 본문 생성 실패 → 기본 본문으로 대체");
                    body = "- Initial project structure created\n- Added core files and configurations\n\n- 프로젝트 초기 구조 생성\n- 핵심 파일 및 설정 추가";
                }
                else
                {
                    progress?.Report($"    ✅ 본문 생성 완료 ({body.Length}자)");
                }

                progress?.Report("🎉 Initial Commit 메시지 생성 완료!");
                return $"<title>{title}</title>\n<body>\n{body}\n</body>";
            }
            finally
            {
                // 작업 후 모델을 VRAM에서 언로드
                await _ollamaClient.UnloadModelAsync();
            }
        }
    }
}
