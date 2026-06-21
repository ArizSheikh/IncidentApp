using Microsoft.AspNetCore.Mvc;
using IncidentApp.AI.MCP;

namespace IncidentApp.Controllers
{
    [ApiController]
    [Route("api/mcp")]
    public class MCPController : ControllerBase
    {
        private readonly MCPServer _mcpServer;
        private readonly MCPToolAdapter _toolAdapter;

        public MCPController(MCPServer mcpServer, MCPToolAdapter toolAdapter)
        {
            _mcpServer = mcpServer;
            _toolAdapter = toolAdapter;
            // Tools are automatically registered in MCPToolAdapter constructor
        }

        [HttpGet("tools")]
        public IActionResult ListTools()
        {
            var tools = _mcpServer.ListTools();
            return Ok(new
            {
                tools = tools.Select(t => new
                {
                    t.Name,
                    t.Description,
                    t.Schema
                })
            });
        }

        [HttpGet("resources")]
        public IActionResult ListResources()
        {
            var resources = _mcpServer.ListResources();
            return Ok(new
            {
                resources = resources.Select(r => new
                {
                    r.Name,
                    r.Description,
                    r.Uri
                })
            });
        }

        [HttpPost("execute/{toolName}")]
        public async Task<IActionResult> ExecuteTool(string toolName, [FromBody] MCPRequest request)
        {
            var response = await _mcpServer.ExecuteToolAsync(toolName, request);
            
            if (!response.Success)
                return BadRequest(response.Error);

            return Ok(response);
        }

        [HttpGet("resource/{resourceName}")]
        public async Task<IActionResult> GetResource(string resourceName)
        {
            var response = await _mcpServer.GetResourceAsync(resourceName);
            
            if (!response.Success)
                return BadRequest(response.Error);

            return Ok(response);
        }
    }
}
