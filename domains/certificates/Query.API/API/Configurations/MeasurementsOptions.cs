using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Configurations;

public class MeasurementsOptions
{
    public const string Measurements = nameof(Measurements);

    [Required]
    public string Url { get; set; } = "";
    [Required]
    public string GrpcUrl { get; set; } = "";
}

public static partial class OptionsExtensions
{
    public static void AddMeasurementsOptions(this IServiceCollection services) =>
        services.AddOptions<MeasurementsOptions>()
            .BindConfiguration(MeasurementsOptions.Measurements)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
