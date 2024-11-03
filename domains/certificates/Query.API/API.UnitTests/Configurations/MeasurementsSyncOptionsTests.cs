using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using API.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.UnitTests.Configurations;

public class MeasurementsSyncOptionsTests
{
    private const string MustBeExplicitlySetError = "The MinimumAgeThresholdHours must be explicitly set.";

    private static List<ValidationResult> ValidateOptions(Dictionary<string, string?> configValues)
    {
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build())
            .BuildServiceProvider();

        var options = new MeasurementsSyncOptions();
        var validationContext = new ValidationContext(options);
        validationContext.InitializeServiceProvider(type => services.GetService(type));

        return options.Validate(validationContext).ToList();
    }

    [Fact]
    public void GivenEnvironmentVariable_WhenMinimumAgeThresholdHoursNotSet_ReturnsValidationError()
    {
        var measurementsSyncOptions = new Dictionary<string, string?>
        {
            [$"{MeasurementsSyncOptions.MeasurementsSync}:Disabled"] = "false",
            [$"{MeasurementsSyncOptions.MeasurementsSync}:SleepType"] = MeasurementsSyncerSleepType.Hourly.ToString(),
        };

        var validationResults = ValidateOptions(measurementsSyncOptions);

        validationResults.Should().ContainSingle()
            .Which.Should().Match<ValidationResult>(vr =>
                vr.ErrorMessage == MustBeExplicitlySetError &&
                vr.MemberNames.Single() == nameof(MeasurementsSyncOptions.MinimumAgeThresholdHours));
    }

    [Fact]
    public void GivenEnvironmentVariable_WhenMinimumAgeThresholdHoursEmptyString_ReturnsValidationError()
    {
        var measurementsSyncOptions = new Dictionary<string, string?>
        {
            [$"{MeasurementsSyncOptions.MeasurementsSync}:Disabled"] = "false",
            [$"{MeasurementsSyncOptions.MeasurementsSync}:SleepType"] = MeasurementsSyncerSleepType.Hourly.ToString(),
            [$"{MeasurementsSyncOptions.MeasurementsSync}:MinimumAgeThresholdHours"] = string.Empty
        };

        var validationResults = ValidateOptions(measurementsSyncOptions);

        validationResults.Should().ContainSingle()
            .Which.Should().Match<ValidationResult>(vr =>
                vr.ErrorMessage == MustBeExplicitlySetError &&
                vr.MemberNames.Single() == nameof(MeasurementsSyncOptions.MinimumAgeThresholdHours));
    }

    [Fact]
    public void GivenEnvironmentVariable_WhenMinimumAgeThresholdHoursExplicitlySetToZero_PassesValidation()
    {
        var measurementsSyncOptions = new Dictionary<string, string?>
        {
            [$"{MeasurementsSyncOptions.MeasurementsSync}:Disabled"] = "false",
            [$"{MeasurementsSyncOptions.MeasurementsSync}:SleepType"] = MeasurementsSyncerSleepType.Hourly.ToString(),
            [$"{MeasurementsSyncOptions.MeasurementsSync}:MinimumAgeThresholdHours"] = "0"
        };

        var validationResults = ValidateOptions(measurementsSyncOptions);

        validationResults.Should().BeEmpty();
    }
}
