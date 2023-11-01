using System;
using System.Linq;
using System.Text.Json.Serialization;
using API.Transfer.Api.Options;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementsAutomation;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using API.Transfer.TransferAgreementsAutomation.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.WalletSystem.V1;

namespace API.Transfer;

public static class Startup
{
    public static void AddTransfer(this IServiceCollection services)
    {
        services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ITransferAgreementRepository, TransferAgreementRepository>();
        services.AddScoped<IProjectOriginWalletService, ProjectOriginWalletService>();
        services.AddScoped<ITransferAgreementHistoryEntryRepository, TransferAgreementHistoryEntryRepository>();
        services.AddGrpcClient<WalletService.WalletServiceClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
            o.Address = new Uri(options.WalletUrl);
        });
        services.AddScoped<ITransferAgreementsAutomationService, TransferAgreementsAutomationService>();
        services.AddHostedService<TransferAgreementsAutomationWorker>();
        services.AddSingleton<AutomationCache>();
        services.AddSingleton<ITransferAgreementAutomationMetrics, TransferAgreementAutomationMetrics>();
    }
}
