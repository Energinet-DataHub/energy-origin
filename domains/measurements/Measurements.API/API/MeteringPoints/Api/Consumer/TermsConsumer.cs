using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Models;
using EnergyOrigin.IntegrationEvents.Events.Terms.V1;
using MassTransit;
using Microsoft.Extensions.Logging;
using Relation.V1;

namespace API.MeteringPoints.Api.Consumer;

public class TermsConsumer(
    ApplicationDbContext dbContext,
    Relation.V1.Relation.RelationClient relationClient,
    ILogger<TermsConsumer> logger)
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
        try
        {
            var res = await relationClient.CreateRelationAsync(request, cancellationToken: CancellationToken.None);
            if (res.Success)
            {
                var relation = dbContext.Relations.SingleOrDefault(it => it.SubjectId == acceptedTerms.SubjectId);
                if (relation != null)
                {
                    relation.Status = RelationStatus.Created;
                    await dbContext.SaveChangesAsync();
                }
            }
            else
            {
                logger.LogWarning("AcceptTerms: Unable to create relations for {SubjectId}. Error: {ErrorMessage}",
                    acceptedTerms.SubjectId, res.ErrorMessage);
            }
        }
        catch (Exception e)
        {
            logger.LogError("AcceptTerms: Unable to create relations for {SubjectId}. Exception: {e}",
                acceptedTerms.SubjectId, e);
        }
    }
}
