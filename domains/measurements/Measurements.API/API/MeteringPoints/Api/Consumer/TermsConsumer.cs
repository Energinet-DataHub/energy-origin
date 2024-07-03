using System;
using System.Threading;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Models;
using API.Options;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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
        if (await dbContext.OrgAcceptedTermsEvents.AnyAsync(x => x.EventId == context.Message.Id))
        {
            return;
        }

        var relationDto =
            await dbContext.Relations.SingleOrDefaultAsync(it => it.SubjectId == context.Message.SubjectId);
        if (relationDto == null)
        {
            relationDto = new RelationDto
            {
                Status = RelationStatus.Pending,
                SubjectId = context.Message.SubjectId,
                Actor = context.Message.Actor,
                Tin = context.Message.Tin
            };

            dbContext.Relations.Add(relationDto);
            await dbContext.SaveChangesAsync();
        }

        if (relationDto.Status == RelationStatus.Pending)
        {
            await CreateRelation(relationDto);
        }
    }

    private async Task CreateRelation(RelationDto relation)
    {
        var request = new CreateRelationRequest
        {
            Subject = relation.SubjectId.ToString(),
            Actor = relation.Actor.ToString(),
            Ssn = "",
            Tin = relation.Tin
        };

        var res = await relationClient.CreateRelationAsync(request, cancellationToken: CancellationToken.None);
        if (res.Success)
        {
            relation.Status = RelationStatus.Created;
            await dbContext.SaveChangesAsync();
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
