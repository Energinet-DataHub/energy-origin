using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Models;
using API.Options;
using EnergyOrigin.IntegrationEvents.Events.Terms.V1;
using Grpc.Core;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relation.V1;

namespace API.MeteringPoints.Api.Consumer;

public class TermsConsumer(
    ApplicationDbContext dbContext,
    Relation.V1.Relation.RelationClient relationClient)
    : IConsumer<OrgAcceptedTerms>
{
    public async Task Consume(ConsumeContext<OrgAcceptedTerms> context)
    {
        var relation = new RelationDto
        {
            Status = RelationStatus.Pending,
            SubjectId = context.Message.SubjectId,
            Actor = context.Message.Actor,
            Tin = context.Message.Tin
        };

        dbContext.Relations.Add(relation);
        await dbContext.SaveChangesAsync();

        await CreateRelation(context.Message);
    }

    private async Task CreateRelation(OrgAcceptedTerms acceptedTerms)
    {
        var request = new CreateRelationRequest
        {
            Subject = acceptedTerms.SubjectId.ToString(),
            Actor = acceptedTerms.Actor.ToString(),
            Ssn = "",
            Tin = acceptedTerms.Tin
        };

        var res = await relationClient.CreateRelationAsync(request, cancellationToken: CancellationToken.None);
        if (res.Success)
        {
            var relation = await dbContext.Relations.SingleOrDefaultAsync(it => it.SubjectId == acceptedTerms.SubjectId);
            if (relation != null)
            {
                relation.Status = RelationStatus.Created;
                await dbContext.SaveChangesAsync();
            }
        }
    }
}

public class TermsConsumerErrorDefinition : ConsumerDefinition<TermsConsumer>
{
    private readonly RetryOptions _retryOptions;

    public TermsConsumerErrorDefinition(IOptions<RetryOptions> retryOptions)
    {
        _retryOptions = retryOptions.Value;
    }
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TermsConsumer> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
