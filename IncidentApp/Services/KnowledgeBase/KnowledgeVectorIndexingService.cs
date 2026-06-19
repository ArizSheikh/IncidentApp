using IncidentApp.AI.VectorSearch;
using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeVectorIndexingService
    {
        private readonly QdrantVectorSearchService _vectorSearchService;
        private readonly KnowledgeEmbeddingService _embeddingService;
        private readonly IKnowledgeRepository _knowledgeRepository;
        private const string CollectionName = "knowledge-base";

        public KnowledgeVectorIndexingService(
            QdrantVectorSearchService vectorSearchService,
            KnowledgeEmbeddingService embeddingService,
            IKnowledgeRepository knowledgeRepository)
        {
            _vectorSearchService = vectorSearchService;
            _embeddingService = embeddingService;
            _knowledgeRepository = knowledgeRepository;
        }

        public async Task InitializeCollectionAsync()
        {
            // Initialize the knowledge-base collection with 768 dimensions
            await _vectorSearchService.InitializeCollectionAsync(768);
        }

        public async Task IndexDocumentAsync(int documentId)
        {
            var document = await _knowledgeRepository.GetDocumentByIdAsync(documentId);
            if (document == null)
                throw new InvalidOperationException($"Document with ID {documentId} not found");

            var chunks = await _knowledgeRepository.GetChunksByDocumentIdAsync(documentId);
            var chunksList = chunks.ToList();
            if (!chunksList.Any())
                return;

            // Generate embeddings for all chunks
            var chunkTexts = chunksList.Select(c => c.ChunkText).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts);

            // Index each chunk in Qdrant
            for (int i = 0; i < chunksList.Count; i++)
            {
                var chunk = chunksList[i];
                var embedding = embeddings[i];

                var payload = new Dictionary<string, object>
                {
                    { "documentId", documentId },
                    { "chunkId", chunk.Id },
                    { "chunkIndex", chunk.ChunkIndex },
                    { "title", document.Title },
                    { "category", document.Category },
                    { "source", document.Source }
                };

                await _vectorSearchService.IndexPointAsync(
                    CollectionName,
                    chunk.Id.ToString(),
                    embedding,
                    payload
                );

                // Update chunk to mark embedding as generated
                chunk.EmbeddingGenerated = true;
                await _knowledgeRepository.CreateChunkAsync(chunk);
            }
        }

        public async Task RemoveDocumentAsync(int documentId)
        {
            var chunks = await _knowledgeRepository.GetChunksByDocumentIdAsync(documentId);
            
            foreach (var chunk in chunks)
            {
                await _vectorSearchService.DeletePointAsync(CollectionName, chunk.Id.ToString());
            }
        }

        public async Task UpdateDocumentAsync(int documentId)
        {
            // Remove existing vectors
            await RemoveDocumentAsync(documentId);
            
            // Re-index the document
            await IndexDocumentAsync(documentId);
        }

        public async Task<List<Dictionary<string, object>>> SearchSimilarChunksAsync(string query, int limit = 5, float scoreThreshold = 0.7f)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
            
            var results = await _vectorSearchService.SearchSimilarVectorsAsync(
                CollectionName,
                queryEmbedding,
                limit,
                scoreThreshold
            );

            return results;
        }

        public async Task<bool> CollectionExistsAsync()
        {
            try
            {
                await _vectorSearchService.InitializeCollectionAsync(768);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
