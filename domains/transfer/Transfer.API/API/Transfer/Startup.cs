using System;
using System.Text.Json.Serialization;
using API.Transfer.Api.Options;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementCleanup;
using API.Transfer.TransferAgreementCleanup.Options;
using API.Transfer.TransferAgreementProposalCleanup;
using API.Transfer.TransferAgreementProposalCleanup.Options;
using EnergyOrigin.WalletClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.Transfer;

public static class Startup
{
    public static void AddTransfer(this IServiceCollection services)
    {
        services.AddOptions<TransferAgreementProposalCleanupServiceOptions>()
            .BindConfiguration(TransferAgreementProposalCleanupServiceOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<TransferAgreementCleanupOptions>()
            .BindConfiguration(TransferAgreementCleanupOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddHttpClient<IWalletClient, WalletClient>((sp, c) =>
        {
            var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
            c.BaseAddress = new Uri(options.WalletUrl);
        });
        services.AddScoped<ITransferAgreementProposalCleanupService, TransferAgreementProposalCleanupService>();
        services.AddSingleton<TransferAgreementStatusService>();
        services.AddHostedService<TransferAgreementProposalCleanupWorker>();
        services.AddHostedService<TransferAgreementCleanupWorker>();
    }
}
