namespace Issuer.Worker.GranularCertificateIssuer;

public static class Startup
{
    public static void AddGranularCertificateIssuer(this IServiceCollection services)
    {
        services.AddHostedService<IssuerWorker>();
    }
}
