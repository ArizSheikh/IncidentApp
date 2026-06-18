using System.Text.Json;

namespace IncidentApp.AI.MCP
{
    public class MCPServer
    {
        private readonly Dictionary<string, MCPTool> _tools = new();
        private readonly Dictionary<string, MCPResource> _resources = new();

        public void RegisterTool(MCPTool tool)
        {
            _tools[tool.Name] = tool;
        }

        public void RegisterResource(MCPResource resource)
        {
            _resources[resource.Name] = resource;
        }

        public async Task<MCPResponse> ExecuteToolAsync(string toolName, MCPRequest request)
        {
            if (!_tools.ContainsKey(toolName))
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = $"Tool '{toolName}' not found"
                };
            }

            var tool = _tools[toolName];
            
            try
            {
                var result = await tool.ExecuteAsync(request.Parameters);
                return new MCPResponse
                {
                    Success = true,
                    Result = result,
                    ToolName = toolName
                };
            }
            catch (Exception ex)
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ToolName = toolName
                };
            }
        }

        public async Task<MCPResponse> GetResourceAsync(string resourceName)
        {
            if (!_resources.ContainsKey(resourceName))
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = $"Resource '{resourceName}' not found"
                };
            }

            var resource = _resources[resourceName];
            
            try
            {
                var result = await resource.GetAsync();
                return new MCPResponse
                {
                    Success = true,
                    Result = result,
                    ResourceName = resourceName
                };
            }
            catch (Exception ex)
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ResourceName = resourceName
                };
            }
        }

        public List<MCPTool> ListTools()
        {
            return _tools.Values.ToList();
        }

        public List<MCPResource> ListResources()
        {
            return _resources.Values.ToList();
        }
    }

    public class MCPTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Schema { get; set; } = new();
        public Func<Dictionary<string, object>, Task<object>> ExecuteAsync { get; set; } = async (args) => new object();
    }

    public class MCPResource
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public Func<Task<object>> GetAsync { get; set; } = async () => new object();
    }

    public class MCPRequest
    {
        public string Method { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class MCPResponse
    {
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public string? ToolName { get; set; }
        public string? ResourceName { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
