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

        // =========================
        // GET ALL INCIDENTS
        // =========================
        public async Task<List<Incident>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        // =========================
        // GET BY ID (FIXED)
        // =========================
        public async Task<Incident> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        // =========================
        // CREATE INCIDENT
        // =========================
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

        // =========================
        // GET CRITICAL INCIDENTS (Stored Proc or DB logic)
        // =========================
        public async Task<List<Incident>> GetCriticalAsync()
        {
            return await _repo.GetBySeveritySP("Critical");
        }

        // =========================
        // SEARCH INCIDENTS (DB-BASED, NOT IN-MEMORY)
        // =========================
        public async Task<List<Incident>> SearchAsync(string query)
        {
            return await _repo.SearchAsync(query);
        }
    }
}