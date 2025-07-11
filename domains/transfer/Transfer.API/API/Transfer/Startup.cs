using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
using Energinet.DataHub.Measurements.Client;
using Energinet.DataHub.Measurements.Client.Extensions.DependencyInjection;
using Energinet.DataHub.Measurements.Client.Extensions.Options;
using Energinet.DataHub.Measurements.Client.ResponseParsers;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Setup;
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

        services.AddEndpointsApiExplorer();
        services.AddVersioningToApi();
        services.AddSwagger("transfer");

        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.DocumentFilter<AddTransferTagDocumentFilter>();
        });

        services.AddHttpContextAccessor();

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
        services.AddScoped<IEnergyDataFormatter, EnergyDataFormatter>();
        services.AddScoped<IMunicipalityPercentageProcessor, MunicipalityPercentageProcessor>();
        services.AddScoped<ICoverageProcessor, CoverageProcessor>();
        services.AddScoped<IEnergySvgRenderer, EnergySvgRenderer>();
        services.AddScoped<IOrganizationHeaderRenderer, OrganizationHeaderRenderer>();
        services.AddScoped<IHeadlinePercentageRenderer, HeadlinePercentageRenderer>();
        services.AddScoped<IMunicipalityPercentageRenderer, MunicipalityPercentageRenderer>();
        services.AddScoped<IOtherCoverageRenderer, OtherCoverageRenderer>();
        services.AddScoped<ILogoRenderer, LogoeRenderer>();
        services.AddScoped<IStyleRenderer, StyleRenderer>();
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

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenService, TokenService>();
        static async Task<AuthenticationHeaderValue> authorizationHeaderProviderAsync(IServiceProvider sp)
        {
            var tokenService = sp.GetRequiredService<ITokenService>();
            var token = await tokenService.GetToken();
            return new AuthenticationHeaderValue("Bearer", token);
        }

        services.AddMeasurementsClient(authorizationHeaderProviderAsync);
        services.AddScoped<IMeasurementClient, MeasurementClient>();
    }
}
