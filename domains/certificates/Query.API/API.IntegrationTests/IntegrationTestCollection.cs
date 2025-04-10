using System;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using DataContext;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace API.IntegrationTests;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = "IntegrationTestCollection";
}

public class IntegrationTestFixture : IAsyncLifetime
{
    public QueryApiWebApplicationFactory WebApplicationFactory { get; private set; }
    public PostgresContainer PostgresContainer { get; private set; }
    private ProjectOriginStack ProjectOriginStack { get; set; }
    public RabbitMqContainer RabbitMqContainer { get; set; }
    public MeasurementsWireMock MeasurementsMock { get; private set; }
    private Respawner? _respawner;

    public IntegrationTestFixture()
    {
        WebApplicationFactory = new QueryApiWebApplicationFactory();
        PostgresContainer = new PostgresContainer();
        ProjectOriginStack = new ProjectOriginStack();
        RabbitMqContainer = new RabbitMqContainer();
        MeasurementsMock = new MeasurementsWireMock();
    }

    public async ValueTask InitializeAsync()
    {
        PostgresContainer = new PostgresContainer();
        await PostgresContainer.InitializeAsync();

        ProjectOriginStack = new ProjectOriginStack();
        await ProjectOriginStack.InitializeAsync();

        RabbitMqContainer = new RabbitMqContainer();
        await RabbitMqContainer.InitializeAsync();

        MeasurementsMock = new MeasurementsWireMock();

        WebApplicationFactory = new QueryApiWebApplicationFactory();
        WebApplicationFactory.ConnectionString = PostgresContainer.ConnectionString;
        WebApplicationFactory.RabbitMqOptions = RabbitMqContainer.Options;
        WebApplicationFactory.MeasurementsUrl = MeasurementsMock.Url;
        WebApplicationFactory.WalletUrl = ProjectOriginStack.WalletUrl;
        WebApplicationFactory.StampUrl = ProjectOriginStack.StampUrl;
        WebApplicationFactory.Start();
        await using var connection = new NpgsqlConnection(WebApplicationFactory.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude =
            [
                "public"
            ],
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("InboxState"),
                new Table("OutboxMessage"),
                new Table("OutboxState")
            ],
            DbAdapter = DbAdapter.Postgres,
        });
    }

    public string WalletUrl => ProjectOriginStack.WalletUrl;

    public async ValueTask DisposeAsync()
    {
        await WebApplicationFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
        MeasurementsMock.Dispose();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("Respawner not initialized yet.");
        if (WebApplicationFactory.ConnectionString is null)
            throw new InvalidOperationException("No test database was created.");

        await using var connection = new NpgsqlConnection(WebApplicationFactory.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(WebApplicationFactory.ConnectionString);
        using var dbContext = new ApplicationDbContext(optionsBuilder.Options);
    }
}
