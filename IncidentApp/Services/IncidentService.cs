using IncidentApp.DTOs;
using IncidentApp.Models;
using IncidentApp.Repositories;

namespace IncidentApp.Services
{
    public class IncidentService
    {
        private readonly IIncidentRepository _repo;

        public IncidentService(IIncidentRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Incident>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Incident> CreateAsync(CreateIncidentDto dto)
        {
            var incident = new Incident
            {
                Title = dto.Title,
                Description = dto.Description,
                Severity = dto.Severity,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(incident);
            return incident;
        }

        public async Task<List<Incident>> GetCriticalAsync()
        {
            return await _repo.GetBySeveritySP("Critical");
        }
    }
}
