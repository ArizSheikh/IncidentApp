using Microsoft.EntityFrameworkCore;
using IncidentApp.Models;

namespace IncidentApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Incident> Incidents { get; set; }
}


