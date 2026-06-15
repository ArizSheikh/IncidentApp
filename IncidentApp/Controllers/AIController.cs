using Microsoft.AspNetCore.Mvc;
using IncidentApp.AI;
using IncidentApp.AI.VectorSearch;
using IncidentApp.Services;

namespace IncidentApp.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly AIOrchestrationService _aiService;
        private readonly QdrantVectorSearchService _vectorSearch;
        private readonly IncidentService _incidentService;

        public AIController(AIOrchestrationService aiService, QdrantVectorSearchService vectorSearch, IncidentService incidentService)
        {
            _aiService = aiService;
            _vectorSearch = vectorSearch;
            _incidentService = incidentService;
        }

        [HttpGet("analyze/{id}")]
        public async Task<IActionResult> Analyze(int id)
        {
            var result = await _aiService.AnalyzeIncidentAsync(id);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            var response = new
            {
                result.Data,
                result.AuditLog
            };

            return Ok(response);
        }

        [HttpPost("index/{id}")]
        public async Task<IActionResult> IndexIncident(int id)
        {
            var success = await _aiService.IndexIncidentForSearchAsync(id);

            if (!success)
                return BadRequest("Failed to index incident");

            return Ok(new { message = "Incident indexed successfully for vector search" });
        }

        [HttpPost("index-all")]
        public async Task<IActionResult> IndexAllIncidents()
        {
            try
            {
                var incidents = await _incidentService.GetAllAsync();
                
                var results = new List<object>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var incident in incidents)
                {
                    var success = await _aiService.IndexIncidentForSearchAsync(incident.Id);
                    
                    if (success)
                    {
                        successCount++;
                        results.Add(new { id = incident.Id, title = incident.Title, status = "success" });
                    }
                    else
                    {
                        failureCount++;
                        results.Add(new { id = incident.Id, title = incident.Title, status = "failed" });
                    }
                }

                return Ok(new 
                { 
                    message = $"Bulk indexing completed: {successCount} succeeded, {failureCount} failed",
                    total = incidents.Count,
                    succeeded = successCount,
                    failed = failureCount,
                    details = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to bulk index incidents: {ex.Message}");
            }
        }

        [HttpPost("initialize-vector-db")]
        public async Task<IActionResult> InitializeVectorDatabase()
        {
            try
            {
                await _vectorSearch.InitializeCollectionAsync();
                return Ok(new { message = "Vector database collection initialized successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to initialize vector database: {ex.Message}");
            }
        }
    }
}
