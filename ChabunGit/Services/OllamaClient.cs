// File: ChabunGit/Services/OllamaClient.cs
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private const string DefaultModel = "gemma4-unsloth:latest";

        public Action<string>? OnLog { get; set; }
        private void Log(string msg) => OnLog?.Invoke(msg);

        public OllamaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateAsync(string prompt, string model = DefaultModel, int maxTokens = 700)
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
                        // ▼▼▼ [핵심 수정] 컨텍스트 크기를 32768로 대폭 확대 ▼▼▼
                        num_ctx = 32768
                        // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Post, "api/generate")
                    {
                        Content = httpContent
                    },
                    HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Ollama API 요청 실패: {response.StatusCode}");

                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sb = new StringBuilder();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                int lineCount = 0;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
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
                            Log("⚠️ [Ollama 경고] 토큰 한도 초과로 응답이 잘렸습니다. num_ctx를 늘려야 합니다.");
                    }
                    catch { }
                }

                string result = sb.ToString();
                Log($"[Ollama 스트림] {lineCount}줄 읽음, 결과 길이: {result.Length}자");

                return string.IsNullOrWhiteSpace(result)
                    ? "Ollama 응답에서 메시지를 찾을 수 없습니다."
                    : result;
            }
            catch (Exception ex)
            {
                return $"Ollama API 호출 중 오류 발생: {ex.Message}";
            }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; } = string.Empty;

            [JsonPropertyName("done")]
            public bool Done { get; set; }

            // ▼▼▼ [추가] done_reason 필드 추가 ▼▼▼
            [JsonPropertyName("done_reason")]
            public string? DoneReason { get; set; }
            // ▲▲▲ [추가] 여기까지 ▲▲▲
        }
    }
}
