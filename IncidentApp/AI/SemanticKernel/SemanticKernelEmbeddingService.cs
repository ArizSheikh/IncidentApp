using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using IncidentApp.AI.Prompts;

namespace IncidentApp.AI.SemanticKernel
{
    public class SemanticKernelEmbeddingService
    {
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly EmbeddingGenerationPrompt _promptBuilder;

        public SemanticKernelEmbeddingService(IConfiguration config)
        {
            var ollamaEndpoint = config["SemanticKernel:OllamaEndpoint"] ?? "http://localhost:11434";
            var ollamaModelId = config["SemanticKernel:OllamaModelId"] ?? "nomic-embed-text";

            var builder = Kernel.CreateBuilder();
            
            // Note: Semantic Kernel doesn't have built-in Ollama support yet
            // We'll use a custom implementation or fall back to OpenAI-compatible endpoint
            // For now, we'll use the existing OllamaEmbeddingService as a fallback
            // or implement a custom text embedding service
            
            _promptBuilder = new EmbeddingGenerationPrompt();
            
            // For this implementation, we'll create a custom embedding service
            // that wraps the existing OllamaEmbeddingService
            _embeddingService = new OllamaTextEmbeddingService(ollamaEndpoint, ollamaModelId);
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
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(new List<string> { text });
            return embeddings.Count > 0 ? embeddings[0].ToArray() : Array.Empty<float>();
        }

        public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(List<string> texts)
        {
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);
            return embeddings.Select(e => e.ToArray()).ToList();
        }
    }

    // Custom implementation to bridge Ollama with Semantic Kernel interface
    public class OllamaTextEmbeddingService : ITextEmbeddingGenerationService
    {
        private readonly string _endpoint;
        private readonly string _modelId;
        private readonly HttpClient _httpClient;

        public OllamaTextEmbeddingService(string endpoint, string modelId)
        {
            _endpoint = endpoint;
            _modelId = modelId;
            _httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
        }

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
        {
            { "model_id", _modelId },
            { "endpoint", _endpoint }
        };

        public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
            IList<string> data,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            var embeddings = new List<ReadOnlyMemory<float>>();

            foreach (var text in data)
            {
                var embedding = await GenerateSingleEmbeddingAsync(text, cancellationToken);
                embeddings.Add(embedding);
            }

            return embeddings;
        }

        private async Task<ReadOnlyMemory<float>> GenerateSingleEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                model = _modelId,
                prompt = text
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/embeddings");
            request.Content = new System.Net.Http.StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama API error: {response.StatusCode} - {json}");
            }

            using var doc = System.Text.Json.JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("embedding", out var embeddingProp))
            {
                throw new Exception("Invalid response format from Ollama API: 'embedding' property missing");
            }

            var embeddings = new List<float>();
            foreach (var element in embeddingProp.EnumerateArray())
            {
                embeddings.Add(element.GetSingle());
            }

            return new ReadOnlyMemory<float>(embeddings.ToArray());
        }
    }
}
