using System.Security.Cryptography;
using System.Text;

namespace IncidentApp.AI.Embedding
{
    public class SimpleEmbeddingService
    {
        public float[] GenerateSimpleEmbedding(string text)
        {
            // Generate a simple hash-based embedding for testing
            // This creates consistent embeddings for the same text
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            
            // Convert to float array and normalize
            var embedding = new float[384]; // Match bge-small-en-v1.5 dimension
            
            for (int i = 0; i < embedding.Length; i++)
            {
                // Use cyclic access to the hash bytes
                var byteValue = bytes[i % bytes.Length];
                // Normalize to range [-1, 1]
                embedding[i] = (byteValue / 127.5f) - 1.0f;
            }
            
            return embedding;
        }

        public Task<float[]> GenerateEmbeddingAsync(
            string incidentTitle,
            string severity,
            string incidentDescription,
            string category,
            string logs,
            string systemComponent)
        {
            var combinedText = $"{incidentTitle} {severity} {incidentDescription} {category} {logs} {systemComponent}";
            return Task.FromResult(GenerateSimpleEmbedding(combinedText));
        }

        public Task<float[]> GenerateEmbeddingFromTextAsync(string text)
        {
            return Task.FromResult(GenerateSimpleEmbedding(text));
        }
    }
}
