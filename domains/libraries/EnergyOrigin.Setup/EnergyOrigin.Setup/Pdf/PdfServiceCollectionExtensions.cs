using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.Setup.Pdf;

public static class PdfServiceCollectionExtensions
{
    public static IServiceCollection AddPdfOptions(this IServiceCollection services)
    {
        services.AddOptions<PdfOptions>()
            .BindConfiguration(PdfOptions.Pdf)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
