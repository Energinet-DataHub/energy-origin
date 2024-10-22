using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

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

    [Required]
    [Range(0, int.MaxValue)]
    public int MinimumAgeThresholdHours { get; set; }
}

public static partial class OptionsExtensions
{
    public static void MeasurementsSyncOptions(this IServiceCollection services) =>
        services.AddOptions<MeasurementsSyncOptions>()
            .BindConfiguration(Configurations.MeasurementsSyncOptions.MeasurementsSync)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
