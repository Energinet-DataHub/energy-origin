using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Configurations;

public class MeasurementsSyncOptions
{
    public const string MeasurementsSync = "MeasurementsSync";

    [Required]
    public bool Disabled { get; set; } = false;
}

public static partial class OptionsExtensions
{
    public static void MeasurementsSyncOptions(this IServiceCollection services) =>
        services.AddOptions<MeasurementsSyncOptions>()
            .BindConfiguration(Configurations.MeasurementsSyncOptions.MeasurementsSync)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
