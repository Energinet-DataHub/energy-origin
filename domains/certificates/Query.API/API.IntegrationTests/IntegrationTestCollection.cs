using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.IntegrationTests.Testcontainers;
using Testing.Testcontainers;
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
    private RabbitMqContainer RabbitMqContainer { get; set; }
    public MeasurementsWireMock MeasurementsMock { get; private set; }
    public RegistryConnectorApplicationFactory RegistryConnectorFactory { get; private set; }

    public IntegrationTestFixture()
    {
        WebApplicationFactory = new QueryApiWebApplicationFactory();
        PostgresContainer = new PostgresContainer();
        ProjectOriginStack = new ProjectOriginStack();
        RabbitMqContainer = new RabbitMqContainer();
        MeasurementsMock = new MeasurementsWireMock();
        RegistryConnectorFactory = new RegistryConnectorApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        PostgresContainer = new PostgresContainer();
        await PostgresContainer.InitializeAsync();

        ProjectOriginStack = new ProjectOriginStack();
        await ProjectOriginStack.InitializeAsync();

        RabbitMqContainer = new RabbitMqContainer();
        await RabbitMqContainer.InitializeAsync();

        MeasurementsMock = new MeasurementsWireMock();

        RegistryConnectorFactory = new RegistryConnectorApplicationFactory();
        RegistryConnectorFactory.RabbitMqOptions = RabbitMqContainer.Options;
        RegistryConnectorFactory.ProjectOriginOptions = ProjectOriginStack.Options;
        RegistryConnectorFactory.ConnectionString = PostgresContainer.ConnectionString;
        RegistryConnectorFactory.Start();

        WebApplicationFactory = new QueryApiWebApplicationFactory();
        WebApplicationFactory.ConnectionString = PostgresContainer.ConnectionString;
        WebApplicationFactory.RabbitMqOptions = RabbitMqContainer.Options;
        WebApplicationFactory.MeasurementsUrl = MeasurementsMock.Url;
        WebApplicationFactory.WalletUrl = ProjectOriginStack.WalletUrl;
        WebApplicationFactory.Start();
    }

    public async Task DisposeAsync()
    {
        await RegistryConnectorFactory.DisposeAsync();
        await WebApplicationFactory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
        await ProjectOriginStack.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
        MeasurementsMock.Dispose();
    }
}
