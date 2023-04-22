using Microsoft.EntityFrameworkCore;
using Notifier.BackgroundService.Host.Database.Entities;

namespace Notifier.BackgroundService.Host.Database;

public class NContext : DbContext
{
    public DbSet<MovieRecord> MoviesRecords { get; set; }
    
    public NContext(DbContextOptions<NContext> options) : base(options)
    {
    }
}