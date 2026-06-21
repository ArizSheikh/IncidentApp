using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace IncidentApp.Models.MCP
{
    public class MCPToolExecutionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ToolName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? AgentName { get; set; }

        public string ArgumentsJson { get; set; } = string.Empty;

        [NotMapped]
        public Dictionary<string, object> Arguments
        {
            get => string.IsNullOrEmpty(ArgumentsJson) ? new() : JsonSerializer.Deserialize<Dictionary<string, object>>(ArgumentsJson) ?? new();
            set => ArgumentsJson = JsonSerializer.Serialize(value);
        }

        public bool Success { get; set; }

        [MaxLength(1000)]
        public string? Error { get; set; }

        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;

        public long DurationMs { get; set; }

        [NotMapped]
        public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

        public int RetryCount { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? CorrelationId { get; set; }
    }

    public class MCPAnalytics
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> ToolUsageCount { get; set; } = new();
        public Dictionary<string, int> AgentUsageCount { get; set; } = new();
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
    }
}
