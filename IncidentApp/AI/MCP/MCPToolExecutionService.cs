using IncidentApp.Services;
using IncidentApp.Services.KnowledgeBase;
using IncidentApp.AI.VectorSearch;
using IncidentApp.AI.SemanticKernel;
using IncidentApp.DTOs;

namespace IncidentApp.AI.MCP
{
    public class MCPToolExecutionService : IMCPToolExecutionService
    {
        private readonly IMCPObservabilityService _observabilityService;
        private readonly IncidentService _incidentService;
        private readonly KnowledgeRetrievalService _knowledgeRetrievalService;
        private readonly QdrantVectorSearchService _vectorSearchService;
        private readonly SemanticKernelService _semanticKernelService;

        private static readonly List<MCPToolInfo> _toolDefinitions = new()
        {
            new MCPToolInfo
            {
                Name = "create_incident",
                Description = "Creates a new incident",
                Category = "Incident",
                RequiredParameters = new List<string> { "title", "description", "severity" }
            },
            new MCPToolInfo
            {
                Name = "search_incidents",
                Description = "Searches incidents by term, severity or status",
                Category = "Incident",
                RequiredParameters = new List<string>()
            },
            new MCPToolInfo
            {
                Name = "update_severity",
                Description = "Updates the severity of an incident",
                Category = "Incident",
                RequiredParameters = new List<string> { "incidentId", "newSeverity" }
            },
            new MCPToolInfo
            {
                Name = "retrieve_historical_incidents",
                Description = "Retrieves historical incidents similar to provided text",
                Category = "Incident",
                RequiredParameters = new List<string> { "currentTitle", "currentDescription" }
            },
            new MCPToolInfo
            {
                Name = "search_knowledge",
                Description = "Searches the knowledge base using semantic search",
                Category = "Knowledge",
                RequiredParameters = new List<string> { "query" }
            }
        };

        public MCPToolExecutionService(
            IMCPObservabilityService observabilityService,
            IncidentService incidentService,
            KnowledgeRetrievalService knowledgeRetrievalService,
            QdrantVectorSearchService vectorSearchService,
            SemanticKernelService semanticKernelService)
        {
            _observabilityService = observabilityService;
            _incidentService = incidentService;
            _knowledgeRetrievalService = knowledgeRetrievalService;
            _vectorSearchService = vectorSearchService;
            _semanticKernelService = semanticKernelService;
        }

        public async Task<MCPToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, string? agentName = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new MCPToolExecutionResult
            {
                ToolName = toolName,
                Arguments = arguments,
                AgentName = agentName,
                ExecutionTime = startTime
            };

            try
            {
                result.Result = toolName switch
                {
                    "create_incident" => await _incidentService.CreateAsync(new CreateIncidentDto
                    {
                        Title = arguments.GetValueOrDefault("title")?.ToString() ?? string.Empty,
                        Description = arguments.GetValueOrDefault("description")?.ToString() ?? string.Empty,
                        Severity = arguments.GetValueOrDefault("severity")?.ToString() ?? string.Empty
                    }),

                    "search_incidents" => (await _incidentService.GetAllAsync()).Where(i =>
                    {
                        var term = arguments.GetValueOrDefault("searchTerm")?.ToString() ?? string.Empty;
                        var sev = arguments.GetValueOrDefault("severity")?.ToString();
                        var status = arguments.GetValueOrDefault("status")?.ToString();
                        return (string.IsNullOrEmpty(term) || i.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || i.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
                            && (string.IsNullOrEmpty(sev) || i.Severity == sev)
                            && (string.IsNullOrEmpty(status) || i.Status == status);
                    }).ToList(),

                    "update_severity" => await HandleUpdateSeverityAsync(arguments),

                    "retrieve_historical_incidents" => (await _incidentService.GetAllAsync())
                        .Take(arguments.ContainsKey("limit") ? Convert.ToInt32(arguments["limit"]) : 5)
                        .ToList(),

                    "search_knowledge" => await _knowledgeRetrievalService.RetrieveRelevantKnowledgeAsync(
                        arguments.GetValueOrDefault("query")?.ToString() ?? string.Empty,
                        arguments.ContainsKey("limit") ? Convert.ToInt32(arguments["limit"]) : 5),

                    _ => throw new NotSupportedException($"Tool '{toolName}' is not supported")
                };

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                await _observabilityService.LogToolExecutionAsync(result);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                await _observabilityService.LogToolExecutionAsync(result);
                return result;
            }
        }

        public Task<List<MCPToolInfo>> DiscoverToolsAsync() => Task.FromResult(_toolDefinitions);

        public Task<bool> ValidateToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            var tool = _toolDefinitions.FirstOrDefault(t => t.Name == toolName);
            if (tool == null) return Task.FromResult(false);
            return Task.FromResult(tool.RequiredParameters.All(p => arguments.ContainsKey(p)));
        }

        public async Task<List<MCPToolInfo>> GetToolsByCategoryAsync(string category)
        {
            var all = await DiscoverToolsAsync();
            return all.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<MCPToolExecutionResult> ExecuteToolWithRetryAsync(string toolName, Dictionary<string, object> arguments, int maxRetries = 3, string? agentName = null)
        {
            for (int i = 1; i <= maxRetries; i++)
            {
                var result = await ExecuteToolAsync(toolName, arguments, agentName);
                result.RetryCount = i - 1;
                if (result.Success) return result;
                await Task.Delay(1000 * i);
            }

            return new MCPToolExecutionResult
            {
                Success = false,
                ToolName = toolName,
                Error = $"Tool execution failed after {maxRetries} retries",
                RetryCount = maxRetries
            };
        }

        private async Task<object?> HandleUpdateSeverityAsync(Dictionary<string, object> arguments)
        {
            var id = Convert.ToInt32(arguments.GetValueOrDefault("incidentId"));
            var incident = await _incidentService.GetByIdAsync(id);
            if (incident == null) return null;
            incident.Severity = arguments.GetValueOrDefault("newSeverity")?.ToString() ?? incident.Severity;
            return incident;
        }
    }
}
