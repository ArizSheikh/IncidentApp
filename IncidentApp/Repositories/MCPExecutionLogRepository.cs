using IncidentApp.Data;
using IncidentApp.Models.MCP;
using Microsoft.EntityFrameworkCore;

namespace IncidentApp.Repositories
{
    public class MCPExecutionLogRepository : IMCPExecutionLogRepository
    {
        private readonly AppDbContext _context;

        public MCPExecutionLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MCPToolExecutionLog> CreateLogAsync(MCPToolExecutionLog log)
        {
            _context.MCPToolExecutionLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<List<MCPToolExecutionLog>> GetLogsAsync(string? toolName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.MCPToolExecutionLogs.AsQueryable();

            if (!string.IsNullOrEmpty(toolName))
            {
                query = query.Where(l => l.ToolName == toolName);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.ExecutionTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.ExecutionTime <= endDate.Value);
            }

            return await query.OrderByDescending(l => l.ExecutionTime).ToListAsync();
        }

        public async Task<MCPAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var logs = await _context.MCPToolExecutionLogs
                .Where(l => l.ExecutionTime >= startDate && l.ExecutionTime <= endDate)
                .ToListAsync();

            var analytics = new MCPAnalytics
            {
                TotalExecutions = logs.Count,
                SuccessfulExecutions = logs.Count(l => l.Success),
                FailedExecutions = logs.Count(l => !l.Success),
                ToolUsageCount = logs.GroupBy(l => l.ToolName).ToDictionary(g => g.Key, g => g.Count()),
                AgentUsageCount = logs.Where(l => !string.IsNullOrEmpty(l.AgentName)).GroupBy(l => l.AgentName!).ToDictionary(g => g.Key, g => g.Count()),
                TotalExecutionTime = TimeSpan.FromMilliseconds(logs.Sum(l => l.DurationMs))
            };

            if (analytics.TotalExecutions > 0)
            {
                analytics.SuccessRate = (double)analytics.SuccessfulExecutions / analytics.TotalExecutions;
                analytics.AverageExecutionTime = TimeSpan.FromMilliseconds(logs.Average(l => l.DurationMs));
            }

            return analytics;
        }
    }
}
