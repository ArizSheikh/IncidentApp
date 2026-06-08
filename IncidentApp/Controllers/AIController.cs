using Microsoft.AspNetCore.Mvc;
using IncidentApp.AI;

namespace IncidentApp.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly AIOrchestrationService _aiService;

        public AIController(AIOrchestrationService aiService)
        {
            _aiService = aiService;
        }

        [HttpGet("analyze/{incidentId}")]
        public async Task<IActionResult> Analyze(int incidentId)
        {
            var result = await _aiService.AnalyzeIncidentAsync(incidentId);
            return Ok(result);
        }
    }
}