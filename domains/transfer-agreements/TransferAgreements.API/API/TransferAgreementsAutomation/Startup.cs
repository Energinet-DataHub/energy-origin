using Microsoft.Extensions.DependencyInjection;

namespace API.TransferAgreementsAutomation;

public static class Startup
{
    public static void AddTransferAgreementsAutomation(this IServiceCollection services)
    {
        services.AddSingleton<TransferAgreementAutomationService>();
        services.AddHostedService<TransferAgreementsAutomationWorker>();
    }
}
