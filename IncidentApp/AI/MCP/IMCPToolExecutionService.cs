namespace IncidentApp.AI.MCP
{
    public interface IMCPToolExecutionService
    {
        Task<MCPToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, string? agentName = null);
        Task<List<MCPToolInfo>> DiscoverToolsAsync();
        Task<bool> ValidateToolAsync(string toolName, Dictionary<string, object> arguments);
        Task<List<MCPToolInfo>> GetToolsByCategoryAsync(string category);
        Task<MCPToolExecutionResult> ExecuteToolWithRetryAsync(string toolName, Dictionary<string, object> arguments, int maxRetries = 3, string? agentName = null);
    }

    public class MCPToolExecutionResult
    {
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public string? ToolName { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new();
        public DateTime ExecutionTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string? AgentName { get; set; }
        public int RetryCount { get; set; }
    }

    public class MCPToolInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, object> Schema { get; set; } = new();
        public Dictionary<string, string> ParameterDescriptions { get; set; } = new();
        public bool RequiresAuthentication { get; set; }
        public List<string> RequiredParameters { get; set; } = new();
    }
}
