using System;
using System.Linq;
using System.Text.Json.Serialization;
using API.Transfer.Api.Options;
using API.Transfer.TransferAgreementCleanup;
using API.Transfer.TransferAgreementCleanup.Options;
using API.Transfer.TransferAgreementProposalCleanup;
using API.Transfer.TransferAgreementProposalCleanup.Options;
using Audit.Core;
using DataContext.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOriginClients;

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

        services.AddControllers(options => options.Filters.Add<AuditDotNetFilter>())
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        Configuration.Setup()
            .UseEntityFramework(ef => ef
                .AuditTypeExplicitMapper(config => config
                    .Map<TransferAgreement, TransferAgreementHistoryEntry>((evt, eventEntry, historyEntity) =>
                    {
                        var actorId = evt.CustomFields.ContainsKey("ActorId")
                            ? evt.CustomFields["ActorId"].ToString()
                            : null;
                        var actorName = evt.CustomFields.ContainsKey("ActorName")
                            ? evt.CustomFields["ActorName"].ToString()
                            : null;

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
                    })
                ));

        services.AddHttpClient<IProjectOriginWalletClient, ProjectOriginWalletClient>((sp, c) =>
        {
            var cvrOptions = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
            c.BaseAddress = new Uri(cvrOptions.WalletUrl + "wallet-api/");
        });
        services.AddScoped<ITransferAgreementProposalCleanupService, TransferAgreementProposalCleanupService>();
        services.AddHostedService<TransferAgreementProposalCleanupWorker>();
        services.AddHostedService<TransferAgreementCleanupWorker>();
    }
}
