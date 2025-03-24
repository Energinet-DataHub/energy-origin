using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace API.MeasurementsSyncer.Clients.DataHubFacade;

public class DataHubFacadeOptions
{
    public const string Prefix = "DataHubFacade";

    [Required]
    public string Url { get; set; } = "";

    [Required]
    public string GrpcUrl { get; set; } = "";
}

public static class OptionsExtensions
{
    public static void AddDataHubFacadeOptions(this IServiceCollection services) =>
        services.AddOptions<DataHubFacadeOptions>()
            .BindConfiguration(DataHubFacadeOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
