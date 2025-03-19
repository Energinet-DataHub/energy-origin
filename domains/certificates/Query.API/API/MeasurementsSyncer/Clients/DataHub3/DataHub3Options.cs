using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace API.MeasurementsSyncer.Clients.DataHub3;
public class DataHub3Options
{
    public const string Prefix = "DataHub3";

    [Required]
    public string Url { get; set; } = "";
    public string? TokenUrl { get; set; } = null!;


    public string? ClientId { get; set; } = null!;
    public string? ClientSecret { get; set; } = null!;

    [Required]
    public bool EnableMock { get; set; } = false;
}

public static class OptionsExtensions
{
    public static void AddDataHub3Options(this IServiceCollection services) =>
        services.AddOptions<DataHub3Options>()
            .BindConfiguration(DataHub3Options.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
