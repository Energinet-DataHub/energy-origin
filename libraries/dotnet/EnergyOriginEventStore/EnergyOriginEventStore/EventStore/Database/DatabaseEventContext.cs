using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EnergyOriginEventStore.EventStore.Database;

#nullable disable

public class DatabaseEventContext : DbContext
{
    public DbSet<Message> Messages { get; set; }

    private readonly string connectionString;

    public DatabaseEventContext(string connectionString) => this.connectionString = connectionString;

    public DatabaseEventContext Clone => new(connectionString);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(Console.WriteLine); // FIXME: weeellll
        optionsBuilder.UseNpgsql(connectionString);
    }
}

[Index(nameof(Topic))]
public class Message
{
    public int Id { get; set; }
    public string Topic { get; set; }
    public string Payload { get; set; }
}
