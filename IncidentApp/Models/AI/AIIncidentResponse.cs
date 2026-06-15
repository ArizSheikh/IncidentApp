namespace IncidentApp.Models.AI
{
    public class AIIncidentResponse
    {
        public string RootCause { get; set; }
        public List<string> MitigationPlan { get; set; }
        public string SeverityAssessment { get; set; }
        public double ConfidenceScore { get; set; }
        public string SimilarIncidentsSummary { get; set; }
        public string Recommendation { get; set; }
    }
}
