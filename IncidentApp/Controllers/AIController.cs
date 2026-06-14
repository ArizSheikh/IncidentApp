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

        [HttpGet("analyze/{id}")]
        public async Task<IActionResult> Analyze(int id)
        {
            var result = await _aiService.AnalyzeIncidentAsync(id);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }
    }
}