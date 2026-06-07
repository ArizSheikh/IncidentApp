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

        public async Task<List<Incident>> GetAllAsync()
        {
            return await _context.Incidents.ToListAsync();
        }

        public async Task AddAsync(Incident incident)
        {
            await _context.Incidents.AddAsync(incident);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Incident>> GetBySeveritySP(string severity)
        {
            var conn = _context.Database.GetConnectionString();
            return await _context.Incidents
                .FromSqlRaw("EXEC GetIncidentsBySeverity @p0", severity)
                .ToListAsync();
        }
    }
}
