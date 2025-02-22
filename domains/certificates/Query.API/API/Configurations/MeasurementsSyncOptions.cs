using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.Configurations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeasurementsSyncerSleepType { EveryThirdSecond, Hourly }

public class MeasurementsSyncOptions
{
    public const string MeasurementsSync = "MeasurementsSync";

    [Required]
    public bool Disabled { get; set; } = false;

    [Required]
    public MeasurementsSyncerSleepType SleepType { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumAgeThresholdHours { get; set; }
}

public static partial class OptionsExtensions
{
    public static void MeasurementsSyncOptions(this IServiceCollection services) =>
        services.AddOptions<MeasurementsSyncOptions>()
            .Configure<IConfiguration>((_, configuration) =>
            {
                var rawValue = configuration[$"{Configurations.MeasurementsSyncOptions.MeasurementsSync}:MinimumAgeThresholdHours"];
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    throw new OptionsValidationException(
                        nameof(MeasurementsSyncOptions),
                        typeof(MeasurementsSyncOptions),
                        ["The MinimumAgeThresholdHours must be explicitly set."
                        ]);
                }
            })
            .BindConfiguration(Configurations.MeasurementsSyncOptions.MeasurementsSync)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
