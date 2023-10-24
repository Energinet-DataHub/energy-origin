using System;
using System.Linq;
using System.Text.Json.Serialization;
using API.Shared;
using API.Transfer.Api.Models;
using API.Transfer.Api.Options;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementsAutomation;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using API.Transfer.TransferAgreementsAutomation.Service;
using Audit.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using ProjectOrigin.WalletSystem.V1;

namespace API.Transfer;

public static class Startup
{
    public static void AddTransfer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ProjectOriginOptions>().BindConfiguration(ProjectOriginOptions.ProjectOrigin).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();

        Configuration.Setup()
            .UseEntityFramework(ef => ef
                .AuditTypeExplicitMapper(config => config
                    .Map<TransferAgreement, TransferAgreementHistoryEntry>((evt, eventEntry, historyEntity) =>
                    {
                        var actorId = evt.CustomFields.ContainsKey("ActorId") ? evt.CustomFields["ActorId"].ToString() : null;
                        var actorName = evt.CustomFields.ContainsKey("ActorName") ? evt.CustomFields["ActorName"].ToString() : null;

                        historyEntity.Id = Guid.NewGuid();
                        historyEntity.CreatedAt = DateTimeOffset.UtcNow;
                        historyEntity.AuditAction = eventEntry.Action;
                        historyEntity.ActorId = actorId ?? string.Empty;
                        historyEntity.ActorName = actorName ?? string.Empty;

                        switch (eventEntry.Action)
                        {
                            case "Insert":
                                historyEntity.TransferAgreementId = (Guid)eventEntry.ColumnValues["Id"];
                                break;
                            case "Update":
                                {
                                    historyEntity.TransferAgreementId = (Guid)eventEntry.PrimaryKey.Values.First();
                                    break;
                                }
                        }
                        return true;
                    })));

        var otlpConfiguration = configuration.GetSection(OtlpOptions.Prefix);
        var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

        services.AddOpenTelemetry()
            .WithMetrics(provider =>
                provider
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TransferAgreementAutomationMetrics.MetricName))
                    .AddMeter(TransferAgreementAutomationMetrics.MetricName)
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = otlpOptions.ReceiverEndpoint));
        services.AddControllers(options => options.Filters.Add<AuditDotNetFilter>())
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
