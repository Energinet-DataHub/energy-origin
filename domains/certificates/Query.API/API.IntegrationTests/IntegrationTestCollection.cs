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
    public RabbitMqContainer RabbitMqContainer { get; set; }
    public MeasurementsWireMock MeasurementsMock { get; private set; }

    public IntegrationTestFixture()
    {
        WebApplicationFactory = new QueryApiWebApplicationFactory();
        PostgresContainer = new PostgresContainer();
        RabbitMqContainer = new RabbitMqContainer();
        MeasurementsMock = new MeasurementsWireMock();
    }

    public async ValueTask InitializeAsync()
    {
        PostgresContainer = new PostgresContainer();
        await PostgresContainer.InitializeAsync();

        RabbitMqContainer = new RabbitMqContainer();
        await RabbitMqContainer.InitializeAsync();

        MeasurementsMock = new MeasurementsWireMock();

        WebApplicationFactory = new QueryApiWebApplicationFactory();
        WebApplicationFactory.ConnectionString = PostgresContainer.ConnectionString;
        WebApplicationFactory.RabbitMqOptions = RabbitMqContainer.Options;
        WebApplicationFactory.MeasurementsUrl = MeasurementsMock.Url;
        WebApplicationFactory.WalletUrl = "http://non-existing"; //ProjectOriginStack.WalletUrl;
        WebApplicationFactory.StampUrl = "http://non-existing"; //ProjectOriginStack.StampUrl;
        WebApplicationFactory.Start();
    }

    public string WalletUrl => "http://non-existing"; //ProjectOriginStack.WalletUrl;

    public async ValueTask DisposeAsync()
    {
        await WebApplicationFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
        MeasurementsMock.Dispose();
    }
}
