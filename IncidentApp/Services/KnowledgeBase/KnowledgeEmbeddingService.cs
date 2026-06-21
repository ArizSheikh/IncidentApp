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
            // Truncate text to prevent exceeding Ollama context window
            const int maxTextLength = 4000;
            var truncatedText = text.Length > maxTextLength ? text.Substring(0, maxTextLength) : text;

            var embedding = await _ollamaService.GenerateEmbeddingAsync(truncatedText, "knowledge", "default", "default", "default", "low");
            if (embedding != null && embedding.Length > 0)
            {
                return embedding;
            }

            throw new InvalidOperationException("Failed to generate embedding using OllamaEmbeddingService.");
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            var results = new float[texts.Count][];
            var semaphore = new SemaphoreSlim(2);

            await Task.WhenAll(texts.Select(async (text, i) =>
            {
                await semaphore.WaitAsync();
                try { results[i] = await GenerateEmbeddingAsync(text); }
                catch (Exception ex) { throw new InvalidOperationException($"Failed to generate embedding for text: {ex.Message}", ex); }
                finally { semaphore.Release(); }
            }));

            return results.ToList();
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
