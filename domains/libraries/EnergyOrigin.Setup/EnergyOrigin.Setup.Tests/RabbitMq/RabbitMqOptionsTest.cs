using EnergyOrigin.Setup.RabbitMq;
using Xunit;

namespace EnergyOrigin.Setup.Tests.RabbitMq;

public class RabbitMqOptionsTest
{
    [Fact]
    public void ParseConnectionString()
    {
        var options = RabbitMqOptions.FromConnectionString("amqp://guest:guest@127.0.0.1:55938/");

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal(55938, options.Port);
        Assert.Equal("guest", options.Username);
        Assert.Equal("guest", options.Password);
    }
}
