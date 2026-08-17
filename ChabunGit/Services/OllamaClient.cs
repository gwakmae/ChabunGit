// File: ChabunGit/Services/OllamaClient.cs
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private const string DefaultModel = "gemma4-unsloth:latest";

        // ▼ 12GB VRAM 기준 안전 범위. 32768 고정은 OOM/CPU 오프로딩의 주범이었음 ▼
        private const int MinContext = 2048;
        private const int MaxSafeContext = 8192;

        // ▼ 빈 응답/일시 오류에 대한 재시도 횟수 ▼
        private const int MaxAttempts = 3;

        // ▼ 토큰 한도 상향 재시도 시 상한 ▼
        private const int MaxPredictTokens = 2048;

        public Action<string>? OnLog { get; set; }
        private void Log(string msg) => OnLog?.Invoke(msg);

        public OllamaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ========================================================================
        // 오류 판별 헬퍼 (PromptService에서 센티넬 문자열 감지에 사용)
        // ========================================================================
        /// <summary>
        /// GenerateAsync의 반환값이 실제 생성 결과가 아니라 오류/실패 메시지인지 판별합니다.
        /// "비어있지 않은 오류 문자열"이 정상 결과처럼 흘러가는 것을 막는 핵심 장치입니다.
        /// </summary>
        public static bool IsErrorResult(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            return text.StartsWith("Ollama API", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("Ollama 응답에서", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOutOfMemoryError(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains("out of memory", StringComparison.OrdinalIgnoreCase)
                || text.Contains("GGML_ASSERT", StringComparison.OrdinalIgnoreCase)
                || text.Contains("cuda", StringComparison.OrdinalIgnoreCase)
                || text.Contains("vram", StringComparison.OrdinalIgnoreCase)
                // GPU 러너 프로세스가 죽은 경우 (간헐적 빈 응답의 주된 원인)
                || text.Contains("runner has unexpectedly stopped", StringComparison.OrdinalIgnoreCase)
                || text.Contains("runner process", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 프롬프트 길이에 맞는 최소한의 컨텍스트 크기를 계산합니다.
        /// 한국어 포함 시 문자당 약 1.5토큰으로 추정하고, 생성 토큰과 안전 여유분을 더합니다.
        /// </summary>
        private static int EstimateContextSize(int promptLength, int maxTokens)
        {
            int estimated = (int)(promptLength * 1.5) + maxTokens + 512;

            // 2의 거듭제곱으로 정렬하면 KV 캐시 할당이 효율적입니다.
            int ctx = MinContext;
            while (ctx < estimated && ctx < MaxSafeContext)
                ctx *= 2;

            return Math.Min(ctx, MaxSafeContext);
        }

        // ========================================================================
        // 공개 생성 API: 실패 유형별 재시도 로직 포함
        // ========================================================================
        public async Task<string> GenerateAsync(
            string prompt,
            string model = DefaultModel,
            int maxTokens = 700,
            CancellationToken cancellationToken = default)
        {
            int ctx = EstimateContextSize(prompt.Length, maxTokens);
            int currentMaxTokens = maxTokens;
            string lastFailure = "Ollama 응답에서 메시지를 찾을 수 없습니다.";

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                Log($"🧠 [시도 {attempt}/{MaxAttempts}] 컨텍스트 {ctx}, 토큰 한도 {currentMaxTokens} (프롬프트 {prompt.Length}자)");

                var result = await GenerateInternalAsync(prompt, model, currentMaxTokens, ctx, cancellationToken);

                if (result.Success)
                    return result.Text;

                lastFailure = result.FailureMessage;

                if (result.Cancelled)
                    return result.FailureMessage;

                // ── 실패 유형별 재시도 전략 ──────────────────────────
                if (result.IsServerError && IsOutOfMemoryError(result.FailureMessage) && ctx > MinContext)
                {
                    // GPU 메모리 부족 → 컨텍스트를 절반으로 줄여 재시도
                    ctx = Math.Max(MinContext, ctx / 2);
                    Log($"⚠️ GPU 메모리 부족 감지. 컨텍스트를 {ctx}로 줄여 재시도합니다.");
                }
                else if (result.WasLengthCut)
                {
                    // thinking 등 사전 출력이 토큰 한도를 다 써서 본문이 빈 경우
                    // → 생성 토큰 한도를 2배로 늘려 재시도
                    currentMaxTokens = Math.Min(currentMaxTokens * 2, MaxPredictTokens);
                    Log($"⚠️ 토큰 한도 안에서 본문이 생성되지 않았습니다. 한도를 {currentMaxTokens}로 늘려 재시도합니다.");
                }
                else if (result.IsServerError)
                {
                    // 러너 크래시 등 일시적 서버 오류 → 러너가 재기동될 시간을 주고 동일 조건 재시도
                    Log($"⚠️ 서버 오류 (러너 재시작 가능성). 잠시 후 동일 조건으로 재시도합니다.");
                }
                else
                {
                    Log($"⚠️ 빈 응답. 잠시 후 재시도합니다.");
                }

                if (attempt < MaxAttempts)
                {
                    try { await Task.Delay(1200, cancellationToken); }
                    catch (OperationCanceledException) { return "Ollama API 호출이 취소되었습니다."; }
                }
            }

            Log($"❌ {MaxAttempts}회 시도 모두 실패.");
            return lastFailure;
        }

        // ========================================================================
        // 1회 생성 시도 (내부용)
        // ========================================================================
        private class AttemptResult
        {
            public bool Success;
            public string Text = string.Empty;
            public string FailureMessage = string.Empty;
            public bool IsServerError;
            public bool WasLengthCut;
            public bool Cancelled;

            public static AttemptResult Ok(string text) => new() { Success = true, Text = text };

            public static AttemptResult Fail(string msg, bool serverError = false, bool lengthCut = false, bool cancelled = false)
                => new()
                {
                    FailureMessage = msg,
                    IsServerError = serverError,
                    WasLengthCut = lengthCut,
                    Cancelled = cancelled
                };
        }

        private async Task<AttemptResult> GenerateInternalAsync(
            string prompt, string model, int maxTokens, int numCtx,
            CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    stream = true,
                    options = new
                    {
                        temperature = 0.2,
                        num_predict = maxTokens,
                        num_ctx = numCtx
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Post, "api/generate")
                    {
                        Content = httpContent
                    },
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    return AttemptResult.Fail(
                        $"Ollama API 요청 실패: {response.StatusCode} - {errorBody}",
                        serverError: true);
                }

                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sb = new StringBuilder();
                bool lengthCut = false;

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                int lineCount = 0;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (!line.TrimStart().StartsWith("{")) continue;

                    lineCount++;
                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OllamaResponse>(line, deserializeOptions);
                        if (chunk == null) continue;

                        // ▼▼▼ [핵심 수정] 스트림 안의 error 필드를 감지합니다.
                        // 기존 코드는 이 라인을 조용히 삼켜서, 서버 오류가
                        // "응답에서 메시지를 찾을 수 없습니다"로 둔갑했습니다. ▼▼▼
                        if (!string.IsNullOrEmpty(chunk.Error))
                        {
                            Log($"❌ Ollama 서버 오류 라인 감지: {chunk.Error}");
                            return AttemptResult.Fail(
                                $"Ollama API 서버 오류: {chunk.Error}",
                                serverError: true);
                        }
                        // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲

                        if (!string.IsNullOrEmpty(chunk.Response))
                            sb.Append(chunk.Response);

                        if (chunk.Done && chunk.DoneReason == "length")
                        {
                            lengthCut = true;
                            Log("⚠️ [Ollama 경고] 토큰 한도 초과로 응답이 잘렸습니다.");
                        }
                    }
                    catch (JsonException) { /* 개별 청크 파싱 실패는 무시 */ }
                }

                string result = sb.ToString();
                Log($"[Ollama 스트림] {lineCount}줄 읽음, 결과 길이: {result.Length}자");

                if (string.IsNullOrWhiteSpace(result))
                {
                    return AttemptResult.Fail(
                        "Ollama 응답에서 메시지를 찾을 수 없습니다.",
                        lengthCut: lengthCut);
                }

                return AttemptResult.Ok(result);
            }
            catch (OperationCanceledException)
            {
                return AttemptResult.Fail("Ollama API 호출이 취소되었습니다.", cancelled: true);
            }
            catch (Exception ex)
            {
                return AttemptResult.Fail($"Ollama API 호출 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 모델을 VRAM에서 즉시 언로드합니다.
        /// keep_alive=0인 빈 요청을내면 Ollama가 모델을 내려서
        /// 다른 프로그램이 GPU 메모리를 사용할 수 있게 됩니다.
        /// </summary>
        public async Task UnloadModelAsync(string model = DefaultModel)
        {
            try
            {
                var requestBody = new { model = model, prompt = "", keep_alive = 0, stream = false };
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync("api/generate", httpContent);
                Log("🧹 AI 모델을 GPU 메모리에서 언로드했습니다.");
            }
            catch (Exception ex)
            {
                Log($"⚠️ 모델 언로드 실패 (무시 가능): {ex.Message}");
            }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; } = string.Empty;

            [JsonPropertyName("done")]
            public bool Done { get; set; }

            [JsonPropertyName("done_reason")]
            public string? DoneReason { get; set; }

            // ▼▼▼ [추가] 스트림 내 서버 오류 필드 ▼▼▼
            [JsonPropertyName("error")]
            public string? Error { get; set; }
            // ▲▲▲ [추가] 여기까지 ▲▲▲
        }
    }
}
