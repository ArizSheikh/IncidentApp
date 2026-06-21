using IncidentApp.AI.MCP;
using IncidentApp.AI.SemanticKernel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IncidentApp.AI.Agents
{
    public class AgentToolSelectionService : IAgentToolSelectionService
    {
        private readonly IMCPToolExecutionService _toolExecutionService;
        private readonly SemanticKernelService _semanticKernelService;
        private readonly IMCPObservabilityService _observabilityService;

        public AgentToolSelectionService(
            IMCPToolExecutionService toolExecutionService,
            SemanticKernelService semanticKernelService,
            IMCPObservabilityService observabilityService)
        {
            _toolExecutionService = toolExecutionService;
            _semanticKernelService = semanticKernelService;
            _observabilityService = observabilityService;
        }

        public async Task<string> SelectToolAsync(string agentName, string userRequest, Dictionary<string, object> context)
        {
            var availableTools = await _toolExecutionService.DiscoverToolsAsync();
            
            var toolSelectionPrompt = $@"You are an intelligent tool selector for the {agentName} agent.

Available tools:
{FormatAvailableTools(availableTools)}

User request: {userRequest}

Context: {FormatContext(context)}

Select the most appropriate tool for this request. Return ONLY the tool name.
If no tool is appropriate, return 'none'.";

            var result = await _semanticKernelService.GetChatCompletionAsync(toolSelectionPrompt);
            var selectedTool = result.ToString().Trim().ToLower()
                .Replace("'", "").Replace("\"", "").Replace("`", "").Split('\n')[0].Trim();

            if (selectedTool == "none" || !availableTools.Any(t => t.Name.Equals(selectedTool, StringComparison.OrdinalIgnoreCase)))
            {
                selectedTool = SelectToolByKeywords(userRequest, availableTools);
            }

            await _observabilityService.LogToolSelectionAsync(agentName, selectedTool, context);
            return selectedTool;
        }

        public async Task<Dictionary<string, object>> ExtractToolArgumentsAsync(string toolName, string userRequest)
        {
            var argumentExtractionPrompt = $@"Extract the arguments for the tool '{toolName}' from the following user request:
{userRequest}

Return the arguments as a JSON object with key-value pairs.
If an argument cannot be determined, use null or an empty string.";

            try
            {
                var result = await _semanticKernelService.GetChatCompletionAsync(argumentExtractionPrompt);
                var argumentsJson = result.ToString()
                    .Replace("```json", "").Replace("```", "").Trim();
                var start = argumentsJson.IndexOf('{');
                var end = argumentsJson.LastIndexOf('}');
                if (start >= 0 && end > start)
                    argumentsJson = argumentsJson.Substring(start, end - start + 1);
                var arguments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson);
                return arguments ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public async Task<bool> ValidateToolSelectionAsync(string agentName, string toolName, string userRequest)
        {
            var validationPrompt = $@"Validate if the tool '{toolName}' is appropriate for the agent '{agentName}' given this request:
{userRequest}

Return 'true' if the tool is appropriate, 'false' otherwise.";

            var result = await _semanticKernelService.GetChatCompletionAsync(validationPrompt);
            var validationResult = result.ToString().Trim().ToLower();
            
            return validationResult == "true" || validationResult.Contains("true");
        }

        private string FormatAvailableTools(List<MCPToolInfo> tools)
        {
            var formatted = new System.Text.StringBuilder();
            foreach (var tool in tools)
            {
                formatted.AppendLine($"- {tool.Name}: {tool.Description}");
                if (tool.RequiredParameters.Any())
                {
                    formatted.AppendLine($"  Required parameters: {string.Join(", ", tool.RequiredParameters)}");
                }
            }
            return formatted.ToString();
        }

        private string FormatContext(Dictionary<string, object> context)
        {
            if (context == null || !context.Any())
                return "No additional context provided.";

            var formatted = new System.Text.StringBuilder();
            foreach (var kvp in context)
            {
                formatted.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            return formatted.ToString();
        }

        private string SelectToolByKeywords(string userRequest, List<MCPToolInfo> availableTools)
        {
            var lowerRequest = userRequest.ToLower();

            if (lowerRequest.Contains("similar") || lowerRequest.Contains("historical") || lowerRequest.Contains("retrieve")
                || lowerRequest.Contains("plan") || lowerRequest.Contains("analysis") || lowerRequest.Contains("analyze"))
                return "retrieve_historical_incidents";

            if (lowerRequest.Contains("search") && lowerRequest.Contains("incident"))
                return "search_incidents";

            if (lowerRequest.Contains("knowledge") || lowerRequest.Contains("documentation") || lowerRequest.Contains("recommend"))
                return "search_knowledge";

            if (lowerRequest.Contains("create") && lowerRequest.Contains("incident"))
                return "create_incident";

            if (lowerRequest.Contains("update") || lowerRequest.Contains("severity"))
                return "update_severity";

            return availableTools.FirstOrDefault()?.Name ?? "retrieve_historical_incidents";
        }
    }
}
