namespace IncidentApp.Models.Governance
{
    public class ModelVersion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string? Endpoint { get; set; }
        public string? Parameters { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public List<EvaluationScore> EvaluationScores { get; set; } = new();
    }
}
