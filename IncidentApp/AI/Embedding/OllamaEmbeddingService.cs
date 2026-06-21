using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IncidentApp.AI.Prompts;

namespace IncidentApp.AI.Embedding
{
    public class OllamaEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly EmbeddingGenerationPrompt _promptBuilder;

        public OllamaEmbeddingService(IConfiguration config)
        {
            _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
            _model = config["Ollama:Model"] ?? "nomic-embed-text";
            
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "IncidentApp/1.0");
            _promptBuilder = new EmbeddingGenerationPrompt();
        }

        public async Task<float[]> GenerateEmbeddingAsync(
            string incidentTitle,
            string severity,
            string incidentDescription,
            string category,
            string logs,
            string systemComponent)
        {
            var prompt = _promptBuilder.GeneratePrompt(
                incidentTitle,
                severity,
                incidentDescription,
                category,
                logs,
                systemComponent
            );

            // Truncate prompt to prevent exceeding Ollama context window
            const int maxPromptLength = 3000;
            var truncatedPrompt = prompt.Length > maxPromptLength ? prompt.Substring(0, maxPromptLength) : prompt;

            return await GenerateEmbeddingFromTextAsync(truncatedPrompt);
        }

        public async Task<float[]> GenerateEmbeddingFromTextAsync(string text)
        {
            try
            {
                // Truncate text if it's too long for the context window
                // Ollama models typically have context limits around 2048-8192 tokens
                // We'll truncate to 4000 characters as a safe limit
                const int maxTextLength = 4000;
                var truncatedText = text.Length > maxTextLength ? text.Substring(0, maxTextLength) : text;

                var requestBody = new { model = _model, prompt = truncatedText };

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/embeddings");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Ollama API error: {response.StatusCode} - {json}");

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("embedding", out var embeddingProp))
                    throw new Exception("Invalid response format from Ollama API: 'embedding' property missing");

                return embeddingProp.EnumerateArray().Select(e => e.GetSingle()).ToArray();
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"Network error calling Ollama API: {httpEx.Message}. Ensure Ollama is running with 'ollama serve'", httpEx);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Ollama API request timed out");
            }
        }

        public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            
            foreach (var text in texts)
            {
                var embedding = await GenerateEmbeddingFromTextAsync(text);
                embeddings.Add(embedding);
            }

            return embeddings;
        }
    }
}
