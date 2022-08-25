using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnergyOriginEventStore.EventStore.Database;

#nullable disable

public class DatabaseEventContext : DbContext
{
    public DbSet<Message> Messages { get; set; }

    public DbSet<Topic> Topics { get; set; }

    private string connectionString;

    public DatabaseEventContext(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(connectionString);
}

public class Message
{
    public int Id { get; set; }
    public Topic Topic { get; set; }
    public string Payload { get; set; }
}

[Index(nameof(Name))]
public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; }
}
