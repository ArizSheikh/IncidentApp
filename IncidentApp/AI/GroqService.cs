using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IncidentApp.AI
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GroqService(IConfiguration config)
        {
            _apiKey = config["Groq:ApiKey"];
            _httpClient = new HttpClient();
        }

        public async Task<string> GetChatCompletionAsync(string prompt)
        {
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = "You are an enterprise incident management AI. Always return structured JSON only." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var result = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result;
        }
    }
}