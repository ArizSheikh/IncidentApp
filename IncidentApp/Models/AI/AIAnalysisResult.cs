namespace IncidentApp.Models.AI
{
    public class AIAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public AIIncidentResponse? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AuditLog { get; set; }
    }
}
