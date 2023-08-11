using API.IntegrationTests.Factories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.IntegrationTests;

public class InvalidConfigurationTests : TestBase, IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly RegistryConnectorApplicationFactory factory;

    public InvalidConfigurationTests(RegistryConnectorApplicationFactory factory)
        => this.factory = factory;

    [Fact]
    public void does_not_start_with_missing_configuration()
    {
        factory.RabbitMqOptions = null;
        factory.ProjectOriginOptions = null;

        var startServer = () => factory.Start();
        startServer.Should().Throw<OptionsValidationException>();
    }
}
