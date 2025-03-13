using EnergyTrackAndTrace.Testing.Testcontainers;
using Worker.IntegrationTests.Factories;
using Xunit;

namespace Worker.IntegrationTests;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = "IntegrationTestCollection";
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public ClaimAutomationWorkerFactory ClaimAutomationWorker { get; private set; }
    public PostgresDatabase PostgresDatabase { get; private set; }

    public IntegrationTestFixture()
    {
        ClaimAutomationWorker = new ClaimAutomationWorkerFactory();
        PostgresDatabase = new PostgresDatabase();
    }

    public async Task InitializeAsync()
    {
        await PostgresDatabase.InitializeAsync();

        ClaimAutomationWorker = new ClaimAutomationWorkerFactory();
        var db = await PostgresDatabase.CreateNewDatabase();
        ClaimAutomationWorker.Database = db;
        ClaimAutomationWorker.Start();
    }

    public async Task DisposeAsync()
    {
        await ClaimAutomationWorker.DisposeAsync();
        await PostgresDatabase.DisposeAsync();
    }
}
