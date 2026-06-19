using Microsoft.EntityFrameworkCore;
using IncidentApp.Models;
using IncidentApp.Models.Governance;
using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Models.MCP;

namespace IncidentApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Incident> Incidents { get; set; }
    public DbSet<PromptVersion> PromptVersions { get; set; }
    public DbSet<ModelVersion> ModelVersions { get; set; }
    public DbSet<EvaluationScore> EvaluationScores { get; set; }
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
    public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }
    public DbSet<MCPToolExecutionLog> MCPToolExecutionLogs { get; set; }
}


