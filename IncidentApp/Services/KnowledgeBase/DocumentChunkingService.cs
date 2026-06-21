using IncidentApp.Models.KnowledgeBase;

namespace IncidentApp.Services.KnowledgeBase
{
    public class DocumentChunkingService
    {
        private const int ChunkSizeWords = 500;
        private const int ChunkOverlapWords = 100;

        public List<KnowledgeChunk> CreateChunks(int documentId, string content)
        {
            var chunks = new List<KnowledgeChunk>();
            
            if (string.IsNullOrWhiteSpace(content))
                return chunks;

            var words = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return chunks;

            var chunkIndex = 0;
            var startPosition = 0;

            while (startPosition < words.Length)
            {
                var endPosition = Math.Min(startPosition + ChunkSizeWords, words.Length);
                var chunkWords = words.Skip(startPosition).Take(endPosition - startPosition).ToArray();
                
                var chunkText = string.Join(" ", chunkWords);
                
                var chunk = new KnowledgeChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = chunkIndex,
                    ChunkText = chunkText,
                    EmbeddingGenerated = false,
                    CreatedDate = DateTime.UtcNow
                };

                chunks.Add(chunk);
                
                // Move to next chunk with overlap
                startPosition += ChunkSizeWords - ChunkOverlapWords;
                chunkIndex++;
            }

            return chunks;
        }

        public int EstimateChunkCount(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            var words = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var effectiveChunkSize = ChunkSizeWords - ChunkOverlapWords;
            
            return (int)Math.Ceiling((double)words.Length / effectiveChunkSize);
        }
    }
}
