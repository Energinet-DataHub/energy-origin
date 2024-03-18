using System;
using System.Linq;
using System.Text.Json.Serialization;
using API.Transfer.Api.Options;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementCleanup;
using API.Transfer.TransferAgreementCleanup.Options;
using API.Transfer.TransferAgreementProposalCleanup;
using API.Transfer.TransferAgreementProposalCleanup.Options;
using Audit.Core;
using DataContext.Repositories;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.WalletSystem.V1;
using Transfer.Application;
using Transfer.Application.Repositories;
using Transfer.Domain;
using Transfer.Domain.Entities;

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

        services.AddScoped<ITransferAgreementRepository, TransferAgreementRepository>();
        services.AddScoped<IProjectOriginWalletService, ProjectOriginWalletService>();
        services.AddScoped<ITransferAgreementHistoryEntryRepository, TransferAgreementHistoryEntryRepository>();
        services.AddScoped<ITransferAgreementProposalRepository, TransferAgreementProposalRepository>();
        services.AddGrpcClient<WalletService.WalletServiceClient>((sp, o) =>
        {
            var options = sp.GetRequiredService<IOptions<ProjectOriginOptions>>().Value;
            o.Address = new Uri(options.WalletUrl);
        });
        services.AddScoped<ITransferAgreementProposalCleanupService, TransferAgreementProposalCleanupService>();
        services.AddHostedService<TransferAgreementProposalCleanupWorker>();
        services.AddHostedService<TransferAgreementCleanupWorker>();

        services.AddScoped<ISystemTime, SystemTime>();

        services.AddScoped<IUserContext, HttpUserContext>();
    }
}

public class HttpUserContext : IUserContext
{
    private readonly UserDescriptor userDescriptor;

    public HttpUserContext(IHttpContextAccessor contextAccessor)
    {
        userDescriptor = new UserDescriptor(contextAccessor.HttpContext?.User);
    }

    public Guid Subject => userDescriptor.Subject;
    public string Name => userDescriptor.Name;
    public Guid OrganizationId => userDescriptor.Organization!.Id;
    public string OrganizationTin => userDescriptor.Organization!.Tin;
    public string OrganizationName => userDescriptor.Organization!.Name;
}
