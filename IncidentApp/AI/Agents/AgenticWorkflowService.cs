using IncidentApp.AI.VectorSearch;
using IncidentApp.Models;
using IncidentApp.Services;

namespace IncidentApp.AI.Agents
{
    public class AgenticWorkflowService
    {
        private readonly PlannerAgent _planner;
        private readonly RetrieverAgent _retriever;
        private readonly AnalyzerAgent _analyzer;
        private readonly RecommendationGeneratorAgent _recommendationGenerator;

        public AgenticWorkflowService(
            IncidentService incidentService,
            QdrantVectorSearchService vectorSearchService)
        {
            _planner = new PlannerAgent();
            _retriever = new RetrieverAgent(incidentService, vectorSearchService);
            _analyzer = new AnalyzerAgent();
            _recommendationGenerator = new RecommendationGeneratorAgent();
        }

        public async Task<AgenticWorkflowResult> ProcessIncidentAsync(int incidentId)
        {
            var workflow = new AgenticWorkflowResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Step 1: Planner - Analyze the incident and create a plan
                workflow.Steps.Add("Planner", await _planner.PlanAnalysisAsync(incidentId));

                // Step 2: Retriever - Fetch relevant historical incidents
                workflow.Steps.Add("Retriever", await _retriever.RetrieveContextAsync(incidentId));

                // Step 3: Analyzer - Analyze the incident with context
                workflow.Steps.Add("Analyzer", await _analyzer.AnalyzeIncidentAsync(workflow.Steps["Planner"], workflow.Steps["Retriever"]));

                // Step 4: Recommendation Generator - Generate actionable recommendations
                workflow.Steps.Add("RecommendationGenerator", await _recommendationGenerator.GenerateRecommendationsAsync(workflow.Steps["Analyzer"]));

                workflow.Success = true;
                workflow.TotalDurationMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                workflow.Success = false;
                workflow.ErrorMessage = ex.Message;
                workflow.TotalDurationMs = stopwatch.ElapsedMilliseconds;
            }

            return workflow;
        }
    }

    public class PlannerAgent
    {
        public async Task<string> PlanAnalysisAsync(int incidentId)
        {
            // Simulate planning logic
            await Task.Delay(100);
            
            var plan = new
            {
                Step = "Planning",
                IncidentId = incidentId,
                AnalysisType = "Root Cause Analysis",
                Priority = "High",
                EstimatedComplexity = "Medium",
                RequiredContext = new[] { "Historical incidents", "System logs", "Similar patterns" },
                NextSteps = new[] { "Retrieve historical data", "Analyze patterns", "Generate recommendations" }
            };

            return System.Text.Json.JsonSerializer.Serialize(plan);
        }
    }

    public class RetrieverAgent
    {
        private readonly IncidentService _incidentService;
        private readonly QdrantVectorSearchService _vectorSearchService;

        public RetrieverAgent(IncidentService incidentService, QdrantVectorSearchService vectorSearchService)
        {
            _incidentService = incidentService;
            _vectorSearchService = vectorSearchService;
        }

        public async Task<string> RetrieveContextAsync(int incidentId)
        {
            var incident = await _incidentService.GetByIdAsync(incidentId);
            if (incident == null)
                return "Incident not found";

            // Use vector search to find similar incidents
            var similarIncidents = await _vectorSearchService.SearchSimilarIncidentsAsync(
                incident.Description ?? string.Empty,
                limit: 5,
                scoreThreshold: 0.6f
            );

            var context = new
            {
                Step = "Retrieval",
                IncidentId = incidentId,
                CurrentIncident = new
                {
                    incident.Title,
                    incident.Description,
                    incident.Severity,
                    incident.Status
                },
                SimilarIncidentsCount = similarIncidents.Count,
                SimilarIncidents = similarIncidents.Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.Description,
                    i.Severity,
                    i.Status
                }).ToList(),
                RetrievalTimestamp = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(context);
        }
    }

    public class AnalyzerAgent
    {
        public async Task<string> AnalyzeIncidentAsync(string planJson, string contextJson)
        {
            await Task.Delay(200);

            var analysis = new
            {
                Step = "Analysis",
                RootCause = "Database connection pool exhaustion",
                ContributingFactors = new[]
                {
                    "High concurrent user load",
                    "Insufficient connection pool size",
                    "Slow query execution"
                },
                SeverityAssessment = "High",
                ConfidenceScore = 0.85,
                PatternMatches = new[]
                {
                    "Similar incident #123 (connection pool issue)",
                    "Similar incident #456 (database timeout)"
                },
                AnalysisTimestamp = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(analysis);
        }
    }

    public class RecommendationGeneratorAgent
    {
        public async Task<string> GenerateRecommendationsAsync(string analysisJson)
        {
            await Task.Delay(150);

            var recommendations = new
            {
                Step = "Recommendation Generation",
                ImmediateActions = new[]
                {
                    "Increase database connection pool size",
                    "Implement connection timeout handling",
                    "Add monitoring for connection pool metrics"
                },
                LongTermActions = new[]
                {
                    "Implement database connection pooling best practices",
                    "Add circuit breaker pattern for database calls",
                    "Optimize slow queries identified in analysis"
                },
                PreventiveMeasures = new[]
                {
                    "Set up alerts for connection pool exhaustion",
                    "Implement load testing for peak scenarios",
                    "Add database performance monitoring"
                },
                EstimatedResolutionTime = "2-4 hours",
                Priority = "High",
                GeneratedAt = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(recommendations);
        }
    }

    public class AgenticWorkflowResult
    {
        public bool Success { get; set; }
        public Dictionary<string, string> Steps { get; set; } = new();
        public long TotalDurationMs { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
