using Microsoft.Extensions.DependencyInjection;
using TransferAgreementsAutomation;

namespace API.TransferAgreementsAutomation;

public static class Startup
{
    public static void AddTransferAgreementsAutomation(this IServiceCollection services)
    {
        services.AddHostedService<TransferAgreementsAutomationWorker>();
    }
}
