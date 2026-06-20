using IncidentApp.Models.MCP;

namespace IncidentApp.Repositories
{
    public interface IMCPExecutionLogRepository
    {
        Task<MCPToolExecutionLog> CreateLogAsync(MCPToolExecutionLog log);
        Task<List<MCPToolExecutionLog>> GetLogsAsync(string? toolName = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<MCPAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);
    }
}
