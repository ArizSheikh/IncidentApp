using IncidentApp.Data;
using IncidentApp.Models.Governance;
using IncidentApp.Models.MCP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentApp.Services
{
    public class AIGovernanceService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AIGovernanceService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Prompt Version Management
        public async Task<PromptVersion> CreatePromptVersionAsync(
            string name, 
            string version, 
            string content, 
            string purpose, 
            string? parameters = null, 
            string? createdBy = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var promptVersion = new PromptVersion
            {
                Name = name,
                Version = version,
                Content = content,
                Purpose = purpose,
                Parameters = parameters,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            context.PromptVersions.Add(promptVersion);
            await context.SaveChangesAsync();

            return promptVersion;
        }

        public async Task<PromptVersion?> GetActivePromptVersionAsync(string name)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            return await context.PromptVersions
                .Where(p => p.Name == name && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PromptVersion>> GetPromptVersionsAsync(string name)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            return await context.PromptVersions
                .Where(p => p.Name == name)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // Model Version Management
        public async Task<ModelVersion> CreateModelVersionAsync(
            string name,
            string version,
            string provider,
            string modelId,
            string? endpoint = null,
            string? parameters = null,
            string? createdBy = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var modelVersion = new ModelVersion
            {
                Name = name,
                Version = version,
                Provider = provider,
                ModelId = modelId,
                Endpoint = endpoint,
                Parameters = parameters,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            context.ModelVersions.Add(modelVersion);
            await context.SaveChangesAsync();

            return modelVersion;
        }

        public async Task<ModelVersion?> GetActiveModelVersionAsync(string name)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            return await context.ModelVersions
                .Where(m => m.Name == name && m.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // Evaluation Score Management
        public async Task<EvaluationScore> RecordEvaluationAsync(
            int promptVersionId,
            int modelVersionId,
            double accuracyScore,
            double relevanceScore,
            double coherenceScore,
            double safetyScore,
            int latencyMs,
            int tokenCount,
            string? evaluationNotes = null,
            string? evaluatedBy = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var overallScore = (accuracyScore + relevanceScore + coherenceScore + safetyScore) / 4.0;

            var evaluationScore = new EvaluationScore
            {
                PromptVersionId = promptVersionId,
                ModelVersionId = modelVersionId,
                AccuracyScore = accuracyScore,
                RelevanceScore = relevanceScore,
                CoherenceScore = coherenceScore,
                SafetyScore = safetyScore,
                OverallScore = overallScore,
                LatencyMs = latencyMs,
                TokenCount = tokenCount,
                EvaluationNotes = evaluationNotes,
                EvaluatedAt = DateTime.UtcNow,
                EvaluatedBy = evaluatedBy
            };

            context.EvaluationScores.Add(evaluationScore);
            await context.SaveChangesAsync();

            return evaluationScore;
        }

        public async Task<List<EvaluationScore>> GetEvaluationScoresAsync(int promptVersionId, int modelVersionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            return await context.EvaluationScores
                .Where(e => e.PromptVersionId == promptVersionId && e.ModelVersionId == modelVersionId)
                .OrderByDescending(e => e.EvaluatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, double>> GetAverageScoresAsync(int promptVersionId, int modelVersionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var scores = await context.EvaluationScores
                .Where(e => e.PromptVersionId == promptVersionId && e.ModelVersionId == modelVersionId)
                .ToListAsync();

            if (!scores.Any())
            {
                return new Dictionary<string, double>();
            }

            return new Dictionary<string, double>
            {
                { "AverageAccuracy", scores.Average(s => s.AccuracyScore) },
                { "AverageRelevance", scores.Average(s => s.RelevanceScore) },
                { "AverageCoherence", scores.Average(s => s.CoherenceScore) },
                { "AverageSafety", scores.Average(s => s.SafetyScore) },
                { "AverageOverall", scores.Average(s => s.OverallScore) },
                { "AverageLatencyMs", scores.Average(s => s.LatencyMs) },
                { "AverageTokenCount", scores.Average(s => s.TokenCount) }
            };
        }

        // MCP Tool Invocation Tracking
        public async Task TrackToolInvocationAsync(
            string toolName,
            string agentName,
            Dictionary<string, object> arguments,
            bool success,
            long durationMs,
            int? retrievedDocumentCount = null,
            Dictionary<int, float>? similarityScores = null,
            int? tokenUsage = null,
            string? error = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var mcpLog = new MCPToolExecutionLog
            {
                ToolName = toolName,
                AgentName = agentName,
                Arguments = arguments,
                Success = success,
                Error = error,
                ExecutionTime = DateTime.UtcNow,
                DurationMs = durationMs,
                RetryCount = 0,
                Category = DetermineToolCategory(toolName)
            };

            context.MCPToolExecutionLogs.Add(mcpLog);
            await context.SaveChangesAsync();

            if (retrievedDocumentCount.HasValue || similarityScores != null)
            {
                await TrackRetrievalMetricsAsync(mcpLog.Id, retrievedDocumentCount, similarityScores);
            }

            if (tokenUsage.HasValue)
            {
                await TrackTokenUsageAsync(mcpLog.Id, tokenUsage.Value);
            }
        }

        private async Task TrackRetrievalMetricsAsync(
            int mcpLogId,
            int? documentCount,
            Dictionary<int, float>? similarityScores)
        {
            // Implementation for tracking retrieval metrics
            // Could be extended to store detailed similarity scores
        }

        private async Task TrackTokenUsageAsync(int mcpLogId, int tokenCount)
        {
            // Implementation for tracking token usage
            // Could be extended to store detailed token metrics
        }

        public async Task<List<MCPToolExecutionLog>> GetToolInvocationHistoryAsync(
            string? toolName = null,
            string? agentName = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var query = context.MCPToolExecutionLogs.AsQueryable();

            if (!string.IsNullOrEmpty(toolName))
                query = query.Where(l => l.ToolName == toolName);

            if (!string.IsNullOrEmpty(agentName))
                query = query.Where(l => l.AgentName == agentName);

            if (startDate.HasValue)
                query = query.Where(l => l.ExecutionTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.ExecutionTime <= endDate.Value);

            return await query.OrderByDescending(l => l.ExecutionTime).ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetGovernanceMetricsAsync(DateTime startDate, DateTime endDate)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var mcpLogs = await context.MCPToolExecutionLogs
                .Where(l => l.ExecutionTime >= startDate && l.ExecutionTime <= endDate)
                .ToListAsync();

            var evaluations = await context.EvaluationScores
                .Where(e => e.EvaluatedAt >= startDate && e.EvaluatedAt <= endDate)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                { "TotalToolInvocations", mcpLogs.Count },
                { "SuccessfulToolInvocations", mcpLogs.Count(l => l.Success) },
                { "FailedToolInvocations", mcpLogs.Count(l => !l.Success) },
                { "AverageToolExecutionTimeMs", mcpLogs.Any() ? mcpLogs.Average(l => l.DurationMs) : 0 },
                { "TotalEvaluations", evaluations.Count },
                { "AverageAccuracyScore", evaluations.Any() ? evaluations.Average(e => e.AccuracyScore) : 0 },
                { "AverageRelevanceScore", evaluations.Any() ? evaluations.Average(e => e.RelevanceScore) : 0 },
                { "AverageLatencyMs", evaluations.Any() ? evaluations.Average(e => e.LatencyMs) : 0 },
                { "TotalTokenCount", evaluations.Sum(e => e.TokenCount) }
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
