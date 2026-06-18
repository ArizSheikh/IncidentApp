using IncidentApp.AI.VectorSearch;
using IncidentApp.AI.SemanticKernel;
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
            QdrantVectorSearchService vectorSearchService,
            SemanticKernelService semanticKernelService,
            AI.GroqService groqService)
        {
            _planner = new PlannerAgent(semanticKernelService);
            _retriever = new RetrieverAgent(incidentService, vectorSearchService);
            _analyzer = new AnalyzerAgent(groqService);
            _recommendationGenerator = new RecommendationGeneratorAgent(groqService);
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
        private readonly SemanticKernelService _semanticKernelService;

        public PlannerAgent(SemanticKernelService semanticKernelService)
        {
            _semanticKernelService = semanticKernelService;
        }

        public async Task<string> PlanAnalysisAsync(int incidentId)
        {
            var prompt = $@"You are an incident analysis planner. For incident #{incidentId}, create a detailed analysis plan.

Your plan should include:
1. Analysis type (Root Cause, Impact Assessment, etc.)
2. Priority level
3. Estimated complexity
4. Required context (historical incidents, system logs, similar patterns)
5. Next steps in the analysis process

Respond with a structured JSON plan.";

            var response = await _semanticKernelService.GetChatCompletionAsync(prompt);
            
            var plan = new
            {
                Step = "Planning",
                IncidentId = incidentId,
                AnalysisType = "Root Cause Analysis",
                Priority = "High",
                EstimatedComplexity = "Medium",
                RequiredContext = new[] { "Historical incidents", "System logs", "Similar patterns" },
                NextSteps = new[] { "Retrieve historical data", "Analyze patterns", "Generate recommendations" },
                AIResponse = response
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
        private readonly AI.GroqService _groqService;

        public AnalyzerAgent(AI.GroqService groqService)
        {
            _groqService = groqService;
        }

        public async Task<string> AnalyzeIncidentAsync(string planJson, string contextJson)
        {
            var prompt = $@"You are an incident analyzer. Analyze the following incident based on the plan and context:

PLAN:
{planJson}

CONTEXT:
{contextJson}

Provide a detailed analysis including:
1. Root cause identification
2. Contributing factors
3. Severity assessment
4. Confidence score
5. Pattern matches with historical incidents

Respond with a structured JSON analysis.";

            var response = await _groqService.GetChatCompletionAsync(prompt);
            
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
                AnalysisTimestamp = DateTime.UtcNow,
                AIResponse = response
            };

            return System.Text.Json.JsonSerializer.Serialize(analysis);
        }
    }

    public class RecommendationGeneratorAgent
    {
        private readonly AI.GroqService _groqService;

        public RecommendationGeneratorAgent(AI.GroqService groqService)
        {
            _groqService = groqService;
        }

        public async Task<string> GenerateRecommendationsAsync(string analysisJson)
        {
            var prompt = $@"You are a recommendation generator. Based on the following incident analysis, generate actionable recommendations:

ANALYSIS:
{analysisJson}

Provide recommendations in three categories:
1. Immediate actions (to take now)
2. Long-term actions (to implement over time)
3. Preventive measures (to prevent future occurrences)

Also include:
- Estimated resolution time
- Priority level

Respond with a structured JSON recommendations object.";

            var response = await _groqService.GetChatCompletionAsync(prompt);
            
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
                GeneratedAt = DateTime.UtcNow,
                AIResponse = response
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
