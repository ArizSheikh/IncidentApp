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

            return await GenerateEmbeddingFromTextAsync(prompt);
        }

        public async Task<float[]> GenerateEmbeddingFromTextAsync(string text)
        {
            try
            {
                Console.WriteLine($"Generating Ollama embedding for text: {text.Substring(0, Math.Min(50, text.Length))}...");
                
                var requestBody = new
                {
                    model = _model,
                    prompt = text
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/embeddings");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Ollama API error: {response.StatusCode} - {json}");
                }

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("embedding", out var embeddingProp))
                {
                    throw new Exception("Invalid response format from Ollama API: 'embedding' property missing");
                }

                var embeddings = new List<float>();
                foreach (var element in embeddingProp.EnumerateArray())
                {
                    embeddings.Add(element.GetSingle());
                }

                Console.WriteLine($"Generated Ollama embedding with {embeddings.Count} dimensions");
                return embeddings.ToArray();
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
                throw new Exception($"Network error calling Ollama API: {httpEx.Message}. Ensure Ollama is running with 'ollama serve'", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine($"Request timeout: {timeoutEx.Message}");
                throw new Exception("Ollama API request timed out", timeoutEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                throw new Exception($"Error generating Ollama embedding: {ex.Message}", ex);
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
