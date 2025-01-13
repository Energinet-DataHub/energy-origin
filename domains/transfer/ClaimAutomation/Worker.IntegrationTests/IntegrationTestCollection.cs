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
    public PostgresContainer PostgresContainer { get; private set; }

    public IntegrationTestFixture()
    {
        ClaimAutomationWorker = new ClaimAutomationWorkerFactory();
        PostgresContainer = new PostgresContainer();
    }

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();

        ClaimAutomationWorker = new ClaimAutomationWorkerFactory();
        var db = await PostgresContainer.CreateNewDatabase();
        ClaimAutomationWorker.Database = db;
        ClaimAutomationWorker.Start();
    }

    public async Task DisposeAsync()
    {
        await ClaimAutomationWorker.DisposeAsync();
        await PostgresContainer.DisposeAsync();
    }
}
