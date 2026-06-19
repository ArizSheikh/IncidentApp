namespace IncidentApp.AI.Agents
{
    public interface IAgentToolSelectionService
    {
        Task<string> SelectToolAsync(string agentName, string userRequest, Dictionary<string, object> context);
        Task<Dictionary<string, object>> ExtractToolArgumentsAsync(string toolName, string userRequest);
        Task<bool> ValidateToolSelectionAsync(string agentName, string toolName, string userRequest);
    }

    public class ToolSelectionResult
    {
        public string SelectedTool { get; set; } = string.Empty;
        public Dictionary<string, object> Arguments { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public DateTime SelectionTime { get; set; } = DateTime.UtcNow;
    }
}
