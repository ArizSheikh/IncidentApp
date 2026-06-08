namespace IncidentApp.Models.AI
{
    public class AIIncidentRawResponse
    {
        public string summary { get; set; }
        public string rootCause { get; set; }
        public List<string> mitigationPlan { get; set; }
        public string severityAssessment { get; set; }
        public double confidenceScore { get; set; }
    }
}