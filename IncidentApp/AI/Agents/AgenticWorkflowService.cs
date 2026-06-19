using IncidentApp.AI.VectorSearch;
using IncidentApp.AI.SemanticKernel;
using IncidentApp.AI.MCP;
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
            IAgentToolSelectionService toolSelectionService,
            IMCPToolExecutionService toolExecutionService)
        {
            _planner = new PlannerAgent(toolSelectionService, toolExecutionService);
            _retriever = new RetrieverAgent(toolSelectionService, toolExecutionService);
            _analyzer = new AnalyzerAgent(toolSelectionService, toolExecutionService);
            _recommendationGenerator = new RecommendationGeneratorAgent(toolSelectionService, toolExecutionService);
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
        private readonly IAgentToolSelectionService _toolSelectionService;
        private readonly IMCPToolExecutionService _toolExecutionService;

        public PlannerAgent(IAgentToolSelectionService toolSelectionService, IMCPToolExecutionService toolExecutionService)
        {
            _toolSelectionService = toolSelectionService;
            _toolExecutionService = toolExecutionService;
        }

        public async Task<string> PlanAnalysisAsync(int incidentId)
        {
            var context = new Dictionary<string, object>
            {
                { "incidentId", incidentId },
                { "task", "planning" }
            };

            var userRequest = $"Create analysis plan for incident #{incidentId}";
            
            // Select appropriate tool using MCP tool selection
            var selectedTool = await _toolSelectionService.SelectToolAsync("PlannerAgent", userRequest, context);
            
            // Extract arguments for the selected tool
            var arguments = await _toolSelectionService.ExtractToolArgumentsAsync(selectedTool, userRequest);
            arguments["incidentId"] = incidentId;

            // Execute tool through MCP
            var executionResult = await _toolExecutionService.ExecuteToolAsync(selectedTool, arguments, "PlannerAgent");

            var plan = new
            {
                Step = "Planning",
                IncidentId = incidentId,
                AnalysisType = "Root Cause Analysis",
                Priority = "High",
                EstimatedComplexity = "Medium",
                RequiredContext = new[] { "Historical incidents", "System logs", "Similar patterns" },
                NextSteps = new[] { "Retrieve historical data", "Analyze patterns", "Generate recommendations" },
                SelectedTool = selectedTool,
                ToolExecutionSuccess = executionResult.Success,
                AIResponse = executionResult.Success ? executionResult.Result?.ToString() : executionResult.Error
            };

            return System.Text.Json.JsonSerializer.Serialize(plan);
        }
    }

    public class RetrieverAgent
    {
        private readonly IAgentToolSelectionService _toolSelectionService;
        private readonly IMCPToolExecutionService _toolExecutionService;

        public RetrieverAgent(IAgentToolSelectionService toolSelectionService, IMCPToolExecutionService toolExecutionService)
        {
            _toolSelectionService = toolSelectionService;
            _toolExecutionService = toolExecutionService;
        }

        public async Task<string> RetrieveContextAsync(int incidentId)
        {
            var context = new Dictionary<string, object>
            {
                { "incidentId", incidentId },
                { "task", "retrieval" }
            };

            var userRequest = $"Retrieve similar historical incidents and knowledge context for incident #{incidentId}";
            
            // Select appropriate tool using MCP tool selection
            var selectedTool = await _toolSelectionService.SelectToolAsync("RetrieverAgent", userRequest, context);
            
            // Extract arguments for the selected tool
            var arguments = await _toolSelectionService.ExtractToolArgumentsAsync(selectedTool, userRequest);
            arguments["incidentId"] = incidentId;

            // Execute tool through MCP
            var executionResult = await _toolExecutionService.ExecuteToolAsync(selectedTool, arguments, "RetrieverAgent");

            var retrievalContext = new
            {
                Step = "Retrieval",
                IncidentId = incidentId,
                SelectedTool = selectedTool,
                ToolExecutionSuccess = executionResult.Success,
                RetrievedData = executionResult.Success ? executionResult.Result : null,
                Error = executionResult.Success ? null : executionResult.Error,
                RetrievalTimestamp = DateTime.UtcNow,
                MCPIntegrationStatus = executionResult.Success ? "Active" : "Fallback"
            };

            return System.Text.Json.JsonSerializer.Serialize(retrievalContext);
        }
    }

    public class AnalyzerAgent
    {
        private readonly IAgentToolSelectionService _toolSelectionService;
        private readonly IMCPToolExecutionService _toolExecutionService;

        public AnalyzerAgent(IAgentToolSelectionService toolSelectionService, IMCPToolExecutionService toolExecutionService)
        {
            _toolSelectionService = toolSelectionService;
            _toolExecutionService = toolExecutionService;
        }

        public async Task<string> AnalyzeIncidentAsync(string planJson, string contextJson)
        {
            var context = new Dictionary<string, object>
            {
                { "plan", planJson },
                { "context", contextJson },
                { "task", "analysis" }
            };

            var userRequest = "Analyze incident with provided plan and context to identify root cause and contributing factors";
            
            // Select appropriate tool using MCP tool selection
            var selectedTool = await _toolSelectionService.SelectToolAsync("AnalyzerAgent", userRequest, context);
            
            // Extract arguments for the selected tool
            var arguments = await _toolSelectionService.ExtractToolArgumentsAsync(selectedTool, userRequest);
            arguments["plan"] = planJson;
            arguments["context"] = contextJson;

            // Execute tool through MCP
            var executionResult = await _toolExecutionService.ExecuteToolAsync(selectedTool, arguments, "AnalyzerAgent");

            var analysis = new
            {
                Step = "Analysis",
                SelectedTool = selectedTool,
                ToolExecutionSuccess = executionResult.Success,
                AnalysisResult = executionResult.Success ? executionResult.Result : null,
                Error = executionResult.Success ? null : executionResult.Error,
                AnalysisTimestamp = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(analysis);
        }
    }

    public class RecommendationGeneratorAgent
    {
        private readonly IAgentToolSelectionService _toolSelectionService;
        private readonly IMCPToolExecutionService _toolExecutionService;

        public RecommendationGeneratorAgent(IAgentToolSelectionService toolSelectionService, IMCPToolExecutionService toolExecutionService)
        {
            _toolSelectionService = toolSelectionService;
            _toolExecutionService = toolExecutionService;
        }

        public async Task<string> GenerateRecommendationsAsync(string analysisJson)
        {
            var context = new Dictionary<string, object>
            {
                { "analysis", analysisJson },
                { "task", "recommendation" }
            };

            var userRequest = "Generate mitigation recommendations based on incident analysis";
            
            // Select appropriate tool using MCP tool selection
            var selectedTool = await _toolSelectionService.SelectToolAsync("RecommendationGeneratorAgent", userRequest, context);
            
            // Extract arguments for the selected tool
            var arguments = await _toolSelectionService.ExtractToolArgumentsAsync(selectedTool, userRequest);
            arguments["analysis"] = analysisJson;

            // Execute tool through MCP
            var executionResult = await _toolExecutionService.ExecuteToolAsync(selectedTool, arguments, "RecommendationGeneratorAgent");

            var recommendations = new
            {
                Step = "Recommendation Generation",
                SelectedTool = selectedTool,
                ToolExecutionSuccess = executionResult.Success,
                Recommendations = executionResult.Success ? executionResult.Result : null,
                Error = executionResult.Success ? null : executionResult.Error,
                RecommendationTimestamp = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(recommendations);
        }
    }

    public class AgenticWorkflowResult
    {
        public bool Success { get; set; }
        public Dictionary<string, string> Steps { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public long TotalDurationMs { get; set; }
    }
}
