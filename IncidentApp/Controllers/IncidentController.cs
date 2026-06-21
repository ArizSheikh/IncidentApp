using IncidentApp.DTOs;
using IncidentApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentApp.Controllers
{
  

    [ApiController]
    [Route("api/incidents")]
    public class IncidentController : ControllerBase
    {
        private readonly IncidentService _service;

        public IncidentController(IncidentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateIncidentDto dto)
        {
            return Ok(await _service.CreateAsync(dto));
        }

        [HttpGet("critical")]
        public async Task<IActionResult> GetCritical()
        {
            return Ok(await _service.GetCriticalAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var incident = await _service.GetByIdAsync(id);

            if (incident == null)
                return NotFound(new { message = "Incident not found" });

            return Ok(incident);
        }
    }
}
