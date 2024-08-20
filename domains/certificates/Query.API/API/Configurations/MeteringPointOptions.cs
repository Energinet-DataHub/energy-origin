using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Configurations;

public class MeteringPointOptions
{
    public const string MeteringPoint = nameof(MeteringPoint);

    [Required]
    public string GrpcUrl { get; set; } = "";
}
public static partial class OptionsExtensions
{
    public static void AddMeteringPointsOptions(this IServiceCollection services) =>
        services.AddOptions<MeteringPointOptions>()
            .BindConfiguration(MeteringPointOptions.MeteringPoint)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
