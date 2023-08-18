using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Configurations;

public class DatasyncOptions
{
    public const string Datasync = "Datasync";

    [Required]
    public string Url { get; set; } = string.Empty;
}

public static partial class OptionsExtensions
{
    public static void AddDatasyncOptions(this IServiceCollection services) =>
        services.AddOptions<DatasyncOptions>()
            .BindConfiguration(DatasyncOptions.Datasync)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
