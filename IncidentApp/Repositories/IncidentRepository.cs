using IncidentApp.Data;
using IncidentApp.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentApp.Repositories
{
    public class IncidentRepository : IIncidentRepository
    {
        private readonly AppDbContext _context;

        public IncidentRepository(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<Incident>> GetAllAsync()
        {
            return await _context.Incidents.ToListAsync();
        }

        // =========================
        // GET BY ID (FIX REQUIRED METHOD)
        // =========================
        public async Task<Incident> GetByIdAsync(int id)
        {
            return await _context.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // =========================
        // ADD INCIDENT
        // =========================
        public async Task AddAsync(Incident incident)
        {
            await _context.Incidents.AddAsync(incident);
            await _context.SaveChangesAsync();
        }

        // =========================
        // SEARCH (FIX REQUIRED METHOD)
        // =========================
        public async Task<List<Incident>> SearchAsync(string query)
        {
            query = query.ToLower();

            return await _context.Incidents
                .Where(x =>
                    x.Title.ToLower().Contains(query) ||
                    x.Description.ToLower().Contains(query) )
                .ToListAsync();
        }

        // =========================
        // STORED PROCEDURE
        // =========================
        public async Task<List<Incident>> GetBySeveritySP(string severity)
        {
            return await _context.Incidents
                .FromSqlRaw("EXEC GetIncidentsBySeverity {0}", severity)
                .ToListAsync();
        }
    }
}