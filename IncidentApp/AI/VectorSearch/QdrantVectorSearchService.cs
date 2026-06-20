using Qdrant.Client;
using Qdrant.Client.Grpc;
using IncidentApp.AI.Prompts;
using IncidentApp.AI.Embedding;
using IncidentApp.AI.SemanticKernel;
using IncidentApp.Models;

namespace IncidentApp.AI.VectorSearch
{
    public class QdrantVectorSearchService
    {
        private readonly QdrantClient _qdrantClient;
        private readonly string _collectionName;
        private readonly VectorSearchPrompt _promptBuilder;
        private readonly SemanticKernelEmbeddingService _embeddingService;

        public QdrantVectorSearchService(
            IConfiguration config,
            SemanticKernelEmbeddingService embeddingService)
        {
            var host = config["Qdrant:Host"] ?? "localhost";
            var port = int.Parse(config["Qdrant:Port"] ?? "6334");
            _collectionName = config["Qdrant:CollectionName"] ?? "incidents";
            
            _qdrantClient = new QdrantClient(host, port);
            _promptBuilder = new VectorSearchPrompt();
            _embeddingService = embeddingService;
        }

        public async Task InitializeCollectionAsync(int vectorSize = 768)
        {
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (collections.Any(c => c == _collectionName))
                return;

            await _qdrantClient.CreateCollectionAsync(
                _collectionName,
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine }
            );
        }

        public async Task<ulong> IndexIncidentAsync(
            ulong incidentId,
            string incidentTitle,
            string severity,
            string incidentDescription,
            string category,
            string logs,
            string systemComponent)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(
                incidentTitle,
                severity,
                incidentDescription,
                category,
                logs,
                systemComponent
            );

            var pointStruct = new PointStruct
            {
                Id = incidentId,
                Vectors = embedding.ToArray(),
                Payload = 
                {
                    ["incident_id"] = incidentId,
                    ["title"] = incidentTitle,
                    ["severity"] = severity,
                    ["description"] = incidentDescription,
                    ["category"] = category,
                    ["logs"] = logs,
                    ["system_component"] = systemComponent,
                    ["indexed_at"] = DateTime.UtcNow.ToString("o")
                }
            };

            await _qdrantClient.UpsertAsync(_collectionName, new List<PointStruct> { pointStruct });

            return incidentId;
        }

        public async Task<List<Incident>> SearchSimilarIncidentsAsync(
            string incidentDescription,
            int limit = 5,
            float scoreThreshold = 0.7f)
        {
            var searchQuery = _promptBuilder.GenerateSearchQuery(incidentDescription);
            var queryEmbedding = await _embeddingService.GenerateEmbeddingFromTextAsync(searchQuery);

            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: _collectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)limit,
                scoreThreshold: scoreThreshold
            );

            var incidents = new List<Incident>();

            foreach (var result in searchResult)
            {
                var payload = result.Payload;
                
                var incident = new Incident
                {
                    Id = payload.TryGetValue("incident_id", out var idValue) ? (int)idValue.IntegerValue : 0,
                    Title = payload.TryGetValue("title", out var titleValue) ? titleValue.StringValue : "",
                    Description = payload.TryGetValue("description", out var descValue) ? descValue.StringValue : "",
                    Severity = payload.TryGetValue("severity", out var severityValue) ? severityValue.StringValue : "",
                    Status = "Resolved",
                    CreatedAt = payload.TryGetValue("indexed_at", out var dateValue) && 
                                DateTime.TryParse(dateValue.StringValue, out var date) ? date : DateTime.UtcNow
                };

                incidents.Add(incident);
            }

            return incidents;
        }

        public async Task DeleteIncidentAsync(ulong incidentId)
        {
            // Delete by using points selector with the correct syntax
            await _qdrantClient.DeleteAsync(
                _collectionName,
                ids: new List<ulong> { incidentId }
            );
        }

        public async Task DeleteCollectionAsync()
        {
            await _qdrantClient.DeleteCollectionAsync(_collectionName);
        }

        // Knowledge chunk indexing methods
        public async Task IndexPointAsync(string collectionName, string pointId, float[] vector, Dictionary<string, object> payload)
        {
            var point = BuildPointStruct(ulong.Parse(pointId), vector, payload);
            await _qdrantClient.UpsertAsync(collectionName, new List<PointStruct> { point });
        }

        public async Task IndexPointsBatchAsync(string collectionName, List<(string pointId, float[] vector, Dictionary<string, object> payload)> points)
        {
            await EnsureCollectionExistsAsync(collectionName, points[0].vector.Length);
            var pointStructs = points.Select(p => BuildPointStruct(ulong.Parse(p.pointId), p.vector, p.payload)).ToList();
            await _qdrantClient.UpsertAsync(collectionName, pointStructs);
        }

        private static readonly SemaphoreSlim _collectionCreateLock = new(1, 1);

        private async Task EnsureCollectionExistsAsync(string collectionName, int vectorSize)
        {
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (collections.Any(c => c == collectionName))
                return;

            await _collectionCreateLock.WaitAsync();
            try
            {
                // re-check inside the lock
                collections = await _qdrantClient.ListCollectionsAsync();
                if (!collections.Any(c => c == collectionName))
                    await _qdrantClient.CreateCollectionAsync(collectionName,
                        new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine });
            }
            finally
            {
                _collectionCreateLock.Release();
            }
        }

        private static PointStruct BuildPointStruct(ulong id, float[] vector, Dictionary<string, object> payload)
        {
            var point = new PointStruct { Id = id, Vectors = vector };
            foreach (var kvp in payload)
            {
                var value = new Value();
                if (kvp.Value is string str) value.StringValue = str;
                else if (kvp.Value is int i) value.IntegerValue = i;
                else if (kvp.Value is long l) value.IntegerValue = l;
                else if (kvp.Value is double d) value.DoubleValue = d;
                else if (kvp.Value is bool b) value.BoolValue = b;
                else value.StringValue = kvp.Value?.ToString() ?? "";
                point.Payload[kvp.Key] = value;
            }
            return point;
        }

        public async Task DeletePointAsync(string collectionName, string pointId)
        {
            await _qdrantClient.DeleteAsync(
                collectionName,
                ids: new List<ulong> { ulong.Parse(pointId) }
            );
        }

        public async Task<List<Dictionary<string, object>>> SearchSimilarVectorsAsync(
            string collectionName,
            float[] queryVector,
            int limit = 5,
            float scoreThreshold = 0.7f)
        {
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: collectionName,
                vector: queryVector,
                limit: (ulong)limit,
                scoreThreshold: scoreThreshold
            );

            var results = new List<Dictionary<string, object>>();

            foreach (var result in searchResult)
            {
                var resultDict = new Dictionary<string, object>
                {
                    { "id", result.Id.ToString() },
                    { "score", result.Score },
                    { "payload", result.Payload.ToDictionary(x => x.Key, x => x.Value) }
                };
                results.Add(resultDict);
            }

            return results;
        }
    }
}
