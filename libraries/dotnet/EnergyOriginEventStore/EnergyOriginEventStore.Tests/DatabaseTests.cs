using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Database;
using EnergyOriginEventStore.Tests.Topics;
using Npgsql;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class DatabaseTests : IAsyncLifetime
{
    private readonly TestcontainerDatabase testcontainers = new TestcontainersBuilder<PostgreSqlTestcontainer>()
      .WithDatabase(new PostgreSqlTestcontainerConfiguration
      {
          Database = "db",
          Username = "postgres",
          Password = "postgres",
      })
      .Build();

    public Task InitializeAsync()
    {
        return this.testcontainers.StartAsync();
    }

    public Task DisposeAsync()
    {
        return this.testcontainers.DisposeAsync().AsTask();
    }

    [Fact]
    public void ExecuteCommand()
    {
        using var connection = new NpgsqlConnection(testcontainers.ConnectionString);
        using var command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText = "SELECT 1";
        command.ExecuteReader();
    }

    [Fact]
    public async Task UseEF()
    {
        var context = new DatabaseEventContext(testcontainers.ConnectionString);

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        context.Messages.Add(new() { Payload = "hello" });

        await context.SaveChangesAsync();

        Assert.True(true);
    }


    [Fact]
    public async Task UseEventStore()
    {
        var context = new DatabaseEventContext(testcontainers.ConnectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // ---

        IEventStore store = new DatabaseEventStore(testcontainers.ConnectionString);

        var message = new Said("Samuel Salesman", "We have been trying to reach you about your cars extended warranty!");
        await store.Produce(message, "Spam", "Advertisement", "Robocall");

        var received = new List<Said>();
        store
            .GetBuilder("Advertisement")
            .AddHandler<Said>(value => received.Add(value.EventModel))
            .Build();

        await Task.Delay(TimeSpan.FromMilliseconds(1000));

        Assert.Single(received);

        Assert.True(true);
    }
}
