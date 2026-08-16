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

        public Action<string>? OnLog { get; set; }
        private void Log(string msg) => OnLog?.Invoke(msg);

        public OllamaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

        public async Task<string> GenerateAsync(
            string prompt,
            string model = DefaultModel,
            int maxTokens = 700,
            CancellationToken cancellationToken = default)
        {
            int ctx = EstimateContextSize(prompt.Length, maxTokens);
            Log($"🧠 컨텍스트 {ctx} 할당 (프롬프트 {prompt.Length}자)");

            string result = await GenerateInternalAsync(prompt, model, maxTokens, ctx, cancellationToken);

            // ▼ OOM 감지 시 컨텍스트를 절반으로 줄여 한 번 자동 재시도 ▼
            if (IsOutOfMemoryError(result) && ctx > MinContext)
            {
                int reducedCtx = Math.Max(MinContext, ctx / 2);
                Log($"⚠️ GPU 메모리 부족 감지. 컨텍스트를 {reducedCtx}로 줄여 재시도합니다.");
                result = await GenerateInternalAsync(prompt, model, maxTokens, reducedCtx, cancellationToken);
            }

            return result;
        }

        private static bool IsOutOfMemoryError(string result)
        {
            return result.Contains("out of memory", StringComparison.OrdinalIgnoreCase)
                || result.Contains("CUDA", StringComparison.OrdinalIgnoreCase)
                || result.Contains("GGML_ASSERT", StringComparison.OrdinalIgnoreCase)
                || (result.Contains("500") && result.Contains("InternalServerError", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<string> GenerateInternalAsync(
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
                    // ▼ 서버 오류 본문을 읽어 OOM 여부를 판별할 수 있게 반환 ▼
                    string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    return $"Ollama API 요청 실패: {response.StatusCode} - {errorBody}";
                }

                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sb = new StringBuilder();

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
                        if (!string.IsNullOrEmpty(chunk?.Response))
                            sb.Append(chunk.Response);

                        // done_reason이 length면 경고 로그
                        if (chunk?.Done == true && chunk?.DoneReason == "length")
                            Log("⚠️ [Ollama 경고] 토큰 한도 초과로 응답이 잘렸습니다.");
                    }
                    catch (JsonException) { /* 개별 청크 파싱 실패는 무시 */ }
                }

                string result = sb.ToString();
                Log($"[Ollama 스트림] {lineCount}줄 읽음, 결과 길이: {result.Length}자");

                return string.IsNullOrWhiteSpace(result)
                    ? "Ollama 응답에서 메시지를 찾을 수 없습니다."
                    : result;
            }
            catch (OperationCanceledException)
            {
                return "Ollama API 호출이 취소되었습니다.";
            }
            catch (Exception ex)
            {
                return $"Ollama API 호출 중 오류 발생: {ex.Message}";
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
        }
    }
}
