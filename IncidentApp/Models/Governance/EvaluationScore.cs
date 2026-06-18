namespace IncidentApp.Models.Governance
{
    public class EvaluationScore
    {
        public int Id { get; set; }
        public int PromptVersionId { get; set; }
        public PromptVersion? PromptVersion { get; set; }
        public int ModelVersionId { get; set; }
        public ModelVersion? ModelVersion { get; set; }
        public double AccuracyScore { get; set; }
        public double RelevanceScore { get; set; }
        public double CoherenceScore { get; set; }
        public double SafetyScore { get; set; }
        public double OverallScore { get; set; }
        public int LatencyMs { get; set; }
        public int TokenCount { get; set; }
        public string? EvaluationNotes { get; set; }
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
        public string? EvaluatedBy { get; set; }
    }
}
