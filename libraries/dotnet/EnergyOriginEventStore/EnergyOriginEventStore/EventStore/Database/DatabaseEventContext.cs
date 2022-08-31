using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EnergyOriginEventStore.EventStore.Database;

#nullable disable

public interface DatabaseEventContext
{
    public DbSet<Message> Messages { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    public DatabaseFacade Database { get; }

    public DatabaseEventContext Clone { get; }
}

public class PostgresDatabaseEventContext : DbContext, DatabaseEventContext // NOTE: dapper needs to be considered as alternative to EF
{
    public DbSet<Message> Messages { get; set; }

    private readonly string connectionString;

    public PostgresDatabaseEventContext(string connectionString) => this.connectionString = connectionString;

    public DatabaseEventContext Clone => new PostgresDatabaseEventContext(connectionString);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(connectionString);
}

[Index(nameof(Topic))]
public class Message
{
    public int Id { get; set; }
    public string Topic { get; set; }
    public string Payload { get; set; }
}
