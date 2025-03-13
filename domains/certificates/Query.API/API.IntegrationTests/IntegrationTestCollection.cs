using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using EnergyTrackAndTrace.Testing.Testcontainers;
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
}
