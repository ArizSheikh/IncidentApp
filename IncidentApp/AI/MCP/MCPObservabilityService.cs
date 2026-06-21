using IncidentApp.Repositories;
using IncidentApp.Models.MCP;

namespace IncidentApp.AI.MCP
{
    public class MCPObservabilityService : IMCPObservabilityService
    {
        private readonly IMCPExecutionLogRepository _logRepository;

        public MCPObservabilityService(IMCPExecutionLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task LogToolExecutionAsync(MCPToolExecutionResult result)
        {
            var log = new MCPToolExecutionLog
            {
                ToolName = result.ToolName ?? string.Empty,
                AgentName = result.AgentName,
                Success = result.Success,
                Error = result.Error,
                ExecutionTime = result.ExecutionTime,
                DurationMs = (long)result.Duration.TotalMilliseconds,
                RetryCount = result.RetryCount,
                Category = DetermineToolCategory(result.ToolName),
                ArgumentsJson = SafeSerialize(result.Arguments)
            };

            await _logRepository.CreateLogAsync(log);
        }

        public async Task LogToolSelectionAsync(string agentName, string selectedTool, Dictionary<string, object> context)
        {
            var log = new MCPToolExecutionLog
            {
                ToolName = selectedTool,
                AgentName = agentName,
                Success = true,
                ExecutionTime = DateTime.UtcNow,
                DurationMs = 0,
                RetryCount = 0,
                Category = DetermineToolCategory(selectedTool),
                ArgumentsJson = SafeSerialize(context)
            };

            await _logRepository.CreateLogAsync(log);
        }

        private static string SafeSerialize(Dictionary<string, object> args)
        {
            try
            {
                var slim = args.ToDictionary(k => k.Key, k => (object)(k.Value?.ToString()?.Length > 200
                    ? k.Value.ToString()!.Substring(0, 200) + "..."
                    : k.Value?.ToString() ?? ""));
                return System.Text.Json.JsonSerializer.Serialize(slim);
            }
            catch { return "{}"; }
        }

        public async Task<List<MCPToolExecutionLog>> GetExecutionHistoryAsync(string? toolName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _logRepository.GetLogsAsync(toolName, startDate, endDate);
        }

        public async Task<MCPAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            return await _logRepository.GetAnalyticsAsync(startDate, endDate);
        }

        private string DetermineToolCategory(string? toolName)
        {
            if (string.IsNullOrEmpty(toolName))
                return "General";

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
