using System;
using System.Text.Json.Serialization;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using API.Transfer.Api.Options;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementCleanup;
using API.Transfer.TransferAgreementCleanup.Options;
using API.Transfer.TransferAgreementProposalCleanup;
using API.Transfer.TransferAgreementProposalCleanup.Options;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.WalletClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.Transfer;

public static class Startup
{
    public static void AddTransfer(this IServiceCollection services)
    {
        services.AddOptions<DataHub3Options>()
            .BindConfiguration(DataHub3Options.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DataHubFacadeOptions>()
            .BindConfiguration(DataHubFacadeOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<TransferAgreementProposalCleanupServiceOptions>()
            .BindConfiguration(TransferAgreementProposalCleanupServiceOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<TransferAgreementCleanupOptions>()
            .BindConfiguration(TransferAgreementCleanupOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin)
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddHttpClient<IWalletClient, EnergyOrigin.WalletClient.WalletClient>((sp, c) =>
        {
            var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
            c.BaseAddress = new Uri(options.WalletUrl);
        });
        services.AddScoped<ITransferAgreementProposalCleanupService, TransferAgreementProposalCleanupService>();
        services.AddSingleton<TransferAgreementStatusService>();
        services.AddHostedService<TransferAgreementProposalCleanupWorker>();
        services.AddHostedService<TransferAgreementCleanupWorker>();

        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IEnergyDataFetcher, EnergyDataFetcher>();
        services.AddScoped<IHeadlinePercentageProcessor, HeadlinePercentageProcessor>();
        services.AddScoped<IEnergySvgRenderer, EnergySvgRenderer>();
        services.AddScoped<IOrganizationHeaderRenderer, OrganizationHeaderRenderer>();
        services.AddScoped<IHeadlinePercentageRenderer, HeadlinePercentageRenderer>();
        services.AddScoped<IConsumptionService, ConsumptionService>();

        services.AddGrpcClient<Meteringpoint.V1.Meteringpoint.MeteringpointClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            o.Address = new Uri(options.GrpcUrl);
        });

        services.AddHttpClient<IDataHubFacadeClient, DataHubFacadeClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHubFacadeOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddTransient<AuthHeaderHandler>();

        services.AddHttpClient<IDataHub3Client, DataHub3Client>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DataHub3Options>>().Value;
            client.BaseAddress = new Uri(options.Url);
            client.Timeout = TimeSpan.FromSeconds(300); // Databricks can autoscale under high load, which can take a long time. So this is so we don't lose the call if that happens.
        }).AddHttpMessageHandler<AuthHeaderHandler>();
    }
}
