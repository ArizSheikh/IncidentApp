using IncidentApp.Models;

namespace IncidentApp.Repositories
{
    public interface IIncidentRepository
    {
        Task<List<Incident>> GetAllAsync();
        Task AddAsync(Incident incident);
        Task<List<Incident>> GetBySeveritySP(string severity);
    }
}
