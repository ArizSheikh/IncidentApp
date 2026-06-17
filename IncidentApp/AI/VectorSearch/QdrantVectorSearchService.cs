using Qdrant.Client;
using Qdrant.Client.Grpc;
using IncidentApp.AI.Prompts;
using IncidentApp.AI.Embedding;
using IncidentApp.Models;

namespace IncidentApp.AI.VectorSearch
{
    public class QdrantVectorSearchService
    {
        private readonly QdrantClient _qdrantClient;
        private readonly string _collectionName;
        private readonly VectorSearchPrompt _promptBuilder;
        private readonly OllamaEmbeddingService _embeddingService;

        public QdrantVectorSearchService(
            IConfiguration config,
            OllamaEmbeddingService embeddingService)
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
            {
                Console.WriteLine($"Collection '{_collectionName}' already exists. Deleting and recreating with {vectorSize} dimensions...");
                await _qdrantClient.DeleteCollectionAsync(_collectionName);
            }
            
            await _qdrantClient.CreateCollectionAsync(
                _collectionName,
                new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine
                }
            );
            Console.WriteLine($"Collection created/recreated with {vectorSize} dimensions");
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
    }
}
