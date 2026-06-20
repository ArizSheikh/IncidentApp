using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;
using IncidentApp.AI.VectorSearch;
using IncidentApp.AI.Embedding;
using Qdrant.Client.Grpc;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeRetrievalService
    {
        private readonly KnowledgeVectorIndexingService _vectorIndexingService;
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly KnowledgeEmbeddingService _embeddingService;
        private const string CollectionName = "knowledge-base";

        public KnowledgeRetrievalService(
            KnowledgeVectorIndexingService vectorIndexingService,
            IKnowledgeRepository knowledgeRepository,
            KnowledgeEmbeddingService embeddingService)
        {
            _vectorIndexingService = vectorIndexingService;
            _knowledgeRepository = knowledgeRepository;
            _embeddingService = embeddingService;
        }

        public async Task<List<Dictionary<string, object>>> SearchKnowledgeAsync(string query, int limit = 5, float scoreThreshold = 0.7f)
        {
            var results = await _vectorIndexingService.SearchSimilarChunksAsync(query, limit, scoreThreshold);
            return results;
        }

        public async Task<KnowledgeRetrievalResult> RetrieveRelevantKnowledgeAsync(string incidentDescription, int limit = 5, float scoreThreshold = 0.3f)
        {
            var searchResults = await _vectorIndexingService.SearchSimilarChunksAsync(incidentDescription, limit, scoreThreshold);

            var knowledgeChunks = new List<KnowledgeChunk>();
            var documents = new List<KnowledgeDocument>();
            var similarityScores = new Dictionary<int, float>();
            var seenDocIds = new HashSet<int>();

            foreach (var result in searchResults)
            {
                if (!result.TryGetValue("payload", out var payloadObj))
                    continue;

                var payload = payloadObj as IDictionary<string, Qdrant.Client.Grpc.Value>;
                if (payload == null) continue;

                if (!payload.TryGetValue("chunkId", out var chunkIdVal)) continue;
                if (!payload.TryGetValue("documentId", out var docIdVal)) continue;

                var chunkId = (int)chunkIdVal.IntegerValue;
                var docId = (int)docIdVal.IntegerValue;

                var chunk = await _knowledgeRepository.GetChunkByIdAsync(chunkId);
                if (chunk == null) continue;

                knowledgeChunks.Add(chunk);

                if (result.TryGetValue("score", out var scoreObj) && scoreObj is float score)
                    similarityScores[chunkId] = score;

                if (seenDocIds.Add(docId))
                {
                    var document = await _knowledgeRepository.GetDocumentByIdAsync(docId);
                    if (document != null)
                        documents.Add(document);
                }
            }

            return new KnowledgeRetrievalResult
            {
                KnowledgeChunks = knowledgeChunks,
                Documents = documents,
                SimilarityScores = similarityScores,
                Query = incidentDescription,
                RetrievedAt = DateTime.UtcNow
            };
        }

        public async Task<List<string>> GetKnowledgeContextAsync(string incidentDescription, int limit = 3)
        {
            var retrievalResult = await RetrieveRelevantKnowledgeAsync(incidentDescription, limit);
            
            var context = new List<string>();
            
            foreach (var document in retrievalResult.Documents)
            {
                context.Add($"Document: {document.Title}");
                context.Add($"Category: {document.Category}");
                context.Add($"Source: {document.Source}");
                context.Add($"Content: {document.Content.Substring(0, Math.Min(500, document.Content.Length))}...");
                context.Add("---");
            }

            return context;
        }
    }

    public class KnowledgeRetrievalResult
    {
        public List<KnowledgeChunk> KnowledgeChunks { get; set; } = new();
        public List<KnowledgeDocument> Documents { get; set; } = new();
        public Dictionary<int, float> SimilarityScores { get; set; } = new();
        public string Query { get; set; } = string.Empty;
        public DateTime RetrievedAt { get; set; }
    }
}
