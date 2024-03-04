using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class TestDbContext : DbContext
{
    public DbSet<ActivityLogEntry> ActivityLogEntries { get; set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }
}
