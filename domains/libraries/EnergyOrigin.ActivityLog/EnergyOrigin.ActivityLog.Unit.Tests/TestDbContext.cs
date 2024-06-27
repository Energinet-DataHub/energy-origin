using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<ActivityLogEntry> ActivityLogEntries { get; set; } = null!;
}
