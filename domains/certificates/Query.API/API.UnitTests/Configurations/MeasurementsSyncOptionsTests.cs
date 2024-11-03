using System.Collections.Generic;
using API.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.UnitTests.Configurations;

public class MeasurementsSyncOptionsTests
{
    private const string MustBeExplicitlySetError = "The MinimumAgeThresholdHours must be explicitly set.";

    private static MeasurementsSyncOptions BuildOptions(Dictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.MeasurementsSyncOptions(); // Use the real options registration logic

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<MeasurementsSyncOptions>>().Value;
    }

    [Fact]
    public void GivenEnvironmentVariable_WhenMinimumAgeThresholdHoursNotSet_ReturnsValidationError()
    {
        var configValues = new Dictionary<string, string?>
        {
            [$"{MeasurementsSyncOptions.MeasurementsSync}:Disabled"] = "false",
            [$"{MeasurementsSyncOptions.MeasurementsSync}:SleepType"] = MeasurementsSyncerSleepType.Hourly.ToString(),
        };

        var exception = Assert.Throws<OptionsValidationException>(() => BuildOptions(configValues));

        exception.Failures.Should().ContainSingle()
            .Which.Should().Be(MustBeExplicitlySetError);
    }

    [Fact]
    public void GivenEnvironmentVariable_WhenMinimumAgeThresholdHoursExplicitlySetToZero_PassesValidation()
    {
        var configValues = new Dictionary<string, string?>
        {
            [$"{MeasurementsSyncOptions.MeasurementsSync}:Disabled"] = "false",
            [$"{MeasurementsSyncOptions.MeasurementsSync}:SleepType"] = MeasurementsSyncerSleepType.Hourly.ToString(),
            [$"{MeasurementsSyncOptions.MeasurementsSync}:MinimumAgeThresholdHours"] = "0"
        };

        var options = BuildOptions(configValues);

        options.MinimumAgeThresholdHours.Should().Be(0);
    }
}
