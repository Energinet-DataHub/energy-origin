using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EnergyOriginEventStore.EventStore.Database;

#nullable disable

public class DatabaseEventContext : DbContext
{
    public DbSet<Message> Messages { get; set; }

    private string connectionString;

    public DatabaseEventContext(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(connectionString);
}

[Index(nameof(Topic))]
public class Message
{
    public int Id { get; set; }
    public string Topic { get; set; }
    public string Payload { get; set; }
}
