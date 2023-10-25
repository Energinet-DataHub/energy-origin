using System;
using System.Linq;
using System.Text.Json.Serialization;
using API.Claiming.Api.Models;
using API.Claiming.Api.Repositories;
using API.Claiming.Automation;
using API.Shared;
using Audit.Core;
using Microsoft.Extensions.DependencyInjection;

namespace API.Claiming;

public static class Startup
{
    public static void AddClaimServices(this IServiceCollection services)
    {
        services.AddScoped<IClaimRepository, ClaimRepository>();

        services.AddScoped<IClaimService, ClaimService>();

        services.AddHostedService<ClaimWorker>();

        Configuration.Setup()
            .UseEntityFramework(ef => ef
                .AuditTypeExplicitMapper(config => config
                    .Map<ClaimSubject, ClaimSubjectHistory>((evt, eventEntry, historyEntity) =>
                    {
                        var subjectId = Guid.Parse(evt.CustomFields["SubjectId"].ToString() ??
                                                   Guid.Empty.ToString());

                        historyEntity.CreatedAt = DateTimeOffset.UtcNow;
                        historyEntity.AuditAction = eventEntry.Action;
                        historyEntity.ActorId = evt.CustomFields["ActorId"].ToString() ?? string.Empty;
                        historyEntity.ActorName = evt.CustomFields["ActorName"].ToString() ?? string.Empty;
                        historyEntity.SubjectId = subjectId;
                        return true;
                    })));
        services.AddControllers(options => options.Filters.Add<AuditDotNetFilter>())
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }
}
