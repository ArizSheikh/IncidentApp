using IncidentApp.Data;
using IncidentApp.Models.Governance;
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
    }
}
