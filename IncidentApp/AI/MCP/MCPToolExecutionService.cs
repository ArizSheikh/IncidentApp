using IncidentApp.Repositories;
using IncidentApp.Services;
using IncidentApp.Services.KnowledgeBase;
using IncidentApp.AI.VectorSearch;
using IncidentApp.AI.SemanticKernel;

namespace IncidentApp.AI.MCP
{
    public class MCPToolExecutionService : IMCPToolExecutionService
    {
        private readonly MCPServer _mcpServer;
        private readonly IMCPObservabilityService _observabilityService;
        private readonly IncidentService _incidentService;
        private readonly KnowledgeRetrievalService _knowledgeRetrievalService;
        private readonly QdrantVectorSearchService _vectorSearchService;
        private readonly SemanticKernelService _semanticKernelService;

        public MCPToolExecutionService(
            MCPServer mcpServer,
            IMCPObservabilityService observabilityService,
            IncidentService incidentService,
            KnowledgeRetrievalService knowledgeRetrievalService,
            QdrantVectorSearchService vectorSearchService,
            SemanticKernelService semanticKernelService)
        {
            _mcpServer = mcpServer;
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
                // Validate tool
                var validation = await ValidateToolAsync(toolName, arguments);
                if (!validation)
                {
                    result.Success = false;
                    result.Error = $"Tool validation failed for: {toolName}";
                    await _observabilityService.LogToolExecutionAsync(result);
                    return result;
                }

                // Execute tool through MCP Server
                var mcpRequest = new MCPRequest
                {
                    Method = toolName,
                    Parameters = arguments
                };

                var mcpResponse = await _mcpServer.ExecuteToolAsync(toolName, mcpRequest);

                result.Success = mcpResponse.Success;
                result.Result = mcpResponse.Result;
                result.Error = mcpResponse.Error;
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

        public async Task<List<MCPToolInfo>> DiscoverToolsAsync()
        {
            var tools = new List<MCPToolInfo>();
            var registeredTools = _mcpServer.ListTools();

            foreach (var tool in registeredTools)
            {
                var toolInfo = new MCPToolInfo
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Schema = tool.Schema,
                    Category = DetermineToolCategory(tool.Name)
                };

                if (tool.Schema.TryGetValue("required", out var requiredObj) && requiredObj is string[] required)
                {
                    toolInfo.RequiredParameters = required.ToList();
                }

                tools.Add(toolInfo);
            }

            return tools;
        }

        public async Task<bool> ValidateToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            var tools = await DiscoverToolsAsync();
            var tool = tools.FirstOrDefault(t => t.Name == toolName);

            if (tool == null)
                return false;

            // Check required parameters
            foreach (var requiredParam in tool.RequiredParameters)
            {
                if (!arguments.ContainsKey(requiredParam))
                    return false;
            }

            return true;
        }

        public async Task<List<MCPToolInfo>> GetToolsByCategoryAsync(string category)
        {
            var allTools = await DiscoverToolsAsync();
            return allTools.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<MCPToolExecutionResult> ExecuteToolWithRetryAsync(string toolName, Dictionary<string, object> arguments, int maxRetries = 3, string? agentName = null)
        {
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    var result = await ExecuteToolAsync(toolName, arguments, agentName);
                    result.RetryCount = retryCount;

                    if (result.Success)
                        return result;

                    lastException = new Exception(result.Error ?? "Unknown error");
                    retryCount++;
                    await Task.Delay(1000 * retryCount); // Exponential backoff
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    await Task.Delay(1000 * retryCount);
                }
            }

            return new MCPToolExecutionResult
            {
                Success = false,
                ToolName = toolName,
                Arguments = arguments,
                AgentName = agentName,
                Error = $"Tool execution failed after {maxRetries} retries: {lastException?.Message}",
                RetryCount = maxRetries
            };
        }

        private string DetermineToolCategory(string toolName)
        {
            if (toolName.Contains("incident"))
                return "Incident";
            if (toolName.Contains("knowledge"))
                return "Knowledge";
            if (toolName.Contains("analyze") || toolName.Contains("recommend"))
                return "AI";
            return "General";
        }
    }
}
