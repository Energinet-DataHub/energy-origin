using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Database;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class DatabaseEventStoreTests : EventStoreTests, IAsyncLifetime
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
        return testcontainers.StartAsync();
    }

    public Task DisposeAsync()
    {
        return testcontainers.DisposeAsync().AsTask();
    }

    public override async Task<IEventStore> buildStore()
    {
        var context = new DatabaseEventContext(testcontainers.ConnectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        return new DatabaseEventStore(testcontainers.ConnectionString);
    }

    public override bool canPersist()
    {
        return true;
    }
}
