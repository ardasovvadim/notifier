using Microsoft.EntityFrameworkCore;
using Notifier.Database.Database.Entities;

namespace Notifier.Database.Database;

public class NContext : DbContext
{
    public DbSet<MovieRecord> MoviesRecords { get; set; }
    public DbSet<User> Users { get; set; }
    
    public NContext(DbContextOptions<NContext> options) : base(options)
    {
    }
}