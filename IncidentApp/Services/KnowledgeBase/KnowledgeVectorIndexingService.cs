using IncidentApp.AI.VectorSearch;
using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeVectorIndexingService
    {
        private readonly QdrantVectorSearchService _vectorSearchService;
        private readonly KnowledgeEmbeddingService _embeddingService;
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private const string CollectionName = "knowledge-base";

        public KnowledgeVectorIndexingService(
            QdrantVectorSearchService vectorSearchService,
            KnowledgeEmbeddingService embeddingService,
            IKnowledgeRepository knowledgeRepository,
            IServiceScopeFactory scopeFactory)
        {
            _vectorSearchService = vectorSearchService;
            _embeddingService = embeddingService;
            _knowledgeRepository = knowledgeRepository;
            _scopeFactory = scopeFactory;
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
            await IndexDocumentAsync(document);
        }

        public async Task IndexDocumentAsync(KnowledgeDocument document)
        {
            var allChunks = document.Chunks.Any()
                ? document.Chunks.ToList()
                : (await _knowledgeRepository.GetChunksByDocumentIdAsync(document.Id)).ToList();

            var chunksList = allChunks.Where(c => !c.EmbeddingGenerated).ToList();
            if (!chunksList.Any())
            {
                Console.WriteLine($"[IndexDocumentAsync] Document {document.Id} has no chunks to index (all chunks already have embeddings)");
                return;
            }

            Console.WriteLine($"[IndexDocumentAsync] Indexing {chunksList.Count} chunks for document {document.Id}");

            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunksList.Select(c => c.ChunkText).ToList());

            var points = chunksList.Select((chunk, i) => (
                pointId: chunk.Id.ToString(),
                vector: embeddings[i],
                payload: new Dictionary<string, object>
                {
                    { "documentId", document.Id },
                    { "chunkId", chunk.Id },
                    { "chunkIndex", chunk.ChunkIndex },
                    { "title", document.Title },
                    { "category", document.Category },
                    { "source", document.Source }
                }
            )).ToList();

            foreach (var chunk in chunksList)
                chunk.EmbeddingGenerated = true;

            var qdrantTask = _vectorSearchService.IndexPointsBatchAsync(CollectionName, points);
            var dbTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();
                await repo.UpdateChunksBatchAsync(chunksList);
            });

            await Task.WhenAll(qdrantTask, dbTask);
            Console.WriteLine($"[IndexDocumentAsync] Successfully indexed {chunksList.Count} chunks for document {document.Id}");
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
            Console.WriteLine($"[SearchSimilarChunksAsync] Searching for: '{query}' with limit={limit}, threshold={scoreThreshold}");
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            var results = await _vectorSearchService.SearchSimilarVectorsAsync(
                CollectionName,
                queryEmbedding,
                limit,
                scoreThreshold
            );

            Console.WriteLine($"[SearchSimilarChunksAsync] Found {results.Count} results");
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
