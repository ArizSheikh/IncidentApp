namespace IncidentApp.AI.MCP
{
    public interface IMCPObservabilityService
    {
        Task LogToolExecutionAsync(MCPToolExecutionResult result);
        Task LogToolSelectionAsync(string agentName, string selectedTool, Dictionary<string, object> context);
        Task<List<Models.MCP.MCPToolExecutionLog>> GetExecutionHistoryAsync(string? toolName = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Models.MCP.MCPAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);
    }
}
