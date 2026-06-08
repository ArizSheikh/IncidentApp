using IncidentApp.Models;

namespace IncidentApp.Repositories
{
    public interface IIncidentRepository
    {
        Task<List<Incident>> GetAllAsync();
        Task<Incident> GetByIdAsync(int id);
        Task AddAsync(Incident incident);
        Task<List<Incident>> SearchAsync(string query);
        Task<List<Incident>> GetBySeveritySP(string severity);
    }
}
