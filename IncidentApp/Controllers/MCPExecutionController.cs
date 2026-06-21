using Microsoft.AspNetCore.Mvc;
using IncidentApp.AI.MCP;

namespace IncidentApp.Controllers
{
    [ApiController]
    [Route("api/mcp/v2")]
    public class MCPExecutionController : ControllerBase
    {
        private readonly IMCPToolExecutionService _toolExecutionService;

        public MCPExecutionController(IMCPToolExecutionService toolExecutionService)
        {
            _toolExecutionService = toolExecutionService;
        }

        [HttpGet("tools")]
        public async Task<IActionResult> GetTools([FromQuery] string? category = null)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    var allTools = await _toolExecutionService.DiscoverToolsAsync();
                    return Ok(allTools);
                }
                else
                {
                    var toolsByCategory = await _toolExecutionService.GetToolsByCategoryAsync(category);
                    return Ok(toolsByCategory);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteTool([FromBody] MCPToolExecutionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ToolName))
                    return BadRequest("Tool name is required");

                if (request.Arguments == null)
                    request.Arguments = new Dictionary<string, object>();

                var result = await _toolExecutionService.ExecuteToolAsync(request.ToolName, request.Arguments, request.AgentName);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        toolName = result.ToolName,
                        result = result.Result,
                        executionTime = result.ExecutionTime,
                        duration = result.Duration,
                        agentName = result.AgentName
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        toolName = result.ToolName,
                        error = result.Error,
                        executionTime = result.ExecutionTime,
                        agentName = result.AgentName
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class MCPToolExecutionRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Arguments { get; set; } = new();
        public string? AgentName { get; set; }
    }
}
