using System;
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
                    stream = false,
                    options = new { temperature = 0.2, num_predict = maxTokens, num_ctx = 8192 }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/generate", httpContent);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Ollama API 요청 실패: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent, deserializeOptions);
                return ollamaResponse?.Response ?? "Ollama 응답에서 메시지를 찾을 수 없습니다.";
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
        }
    }
}