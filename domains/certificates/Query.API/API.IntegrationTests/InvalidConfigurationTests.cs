using API.IntegrationTests.Factories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.IntegrationTests;

public class InvalidConfigurationTests : TestBase, IClassFixture<RegistryConnectorApplicationFactory>, IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly RegistryConnectorApplicationFactory connectorFactory;
    private readonly QueryApiWebApplicationFactory apiFactory;

    public InvalidConfigurationTests(RegistryConnectorApplicationFactory connectorFactory, QueryApiWebApplicationFactory apiFactory)
    {
        this.connectorFactory = connectorFactory;
        this.apiFactory = apiFactory;
    }

    [Fact]
    public void connector_does_not_start_with_missing_configuration()
    {
        connectorFactory.RabbitMqOptions = null;
        connectorFactory.ProjectOriginOptions = null;

        var startServer = () => connectorFactory.Start();
        startServer.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void api_does_not_start_with_missing_configuration()
    {
        apiFactory.RabbitMqOptions = null;
        apiFactory.WalletUrl = string.Empty;
        apiFactory.DataSyncUrl = string.Empty;

        var startServer = () => apiFactory.Start();
        startServer.Should().Throw<OptionsValidationException>();
    }
}
