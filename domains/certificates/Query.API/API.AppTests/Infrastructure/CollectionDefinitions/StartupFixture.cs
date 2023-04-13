using DotNet.Testcontainers.Configurations;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;

namespace API.AppTests.Infrastructure.CollectionDefinitions;

public class StartupFixture
{
    public StartupFixture()
    {
        var loggerConfiguration = new LoggerConfiguration().WriteTo.Console();
        using var provider = new SerilogLoggerProvider(loggerConfiguration.CreateLogger());
        TestcontainersSettings.Logger = provider.CreateLogger("Testcontainers");
    }
}

[CollectionDefinition("Startup")]
public class StartupCollection : ICollectionFixture<StartupFixture>
{
    public const string Name = "Startup";
}
