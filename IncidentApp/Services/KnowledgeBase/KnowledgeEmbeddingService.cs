using IncidentApp.AI.Embedding;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeEmbeddingService
    {
        private readonly OllamaEmbeddingService _ollamaService;

        public KnowledgeEmbeddingService(OllamaEmbeddingService ollamaService)
        {
            _ollamaService = ollamaService;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var embedding = await _ollamaService.GenerateEmbeddingAsync(text, "knowledge", "default", "default", "default", "low");
            if (embedding != null && embedding.Length > 0)
            {
                return embedding;
            }

            throw new InvalidOperationException("Failed to generate embedding using OllamaEmbeddingService.");
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            
            foreach (var text in texts)
            {
                try
                {
                    var embedding = await GenerateEmbeddingAsync(text);
                    embeddings.Add(embedding);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to generate embedding for text: {ex.Message}", ex);
                }
            }

            return embeddings;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var testEmbedding = await _ollamaService.GenerateEmbeddingAsync("test", "knowledge", "default", "default", "default", "low");
                return testEmbedding != null && testEmbedding.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
