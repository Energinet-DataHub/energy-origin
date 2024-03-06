using System;
using System.Threading;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Models;
using EnergyOrigin.IntegrationEvents.Events;
using MassTransit;
using Relation.V1;

namespace API.MeteringPoints.Api.Consumer;

public class TermsConsumer : IConsumer<OrgAcceptedTerms>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Relation.V1.Relation.RelationClient _relationClient;

    public TermsConsumer(ApplicationDbContext dbContext, Relation.V1.Relation.RelationClient relationClient)
    {
        _dbContext = dbContext;
        _relationClient = relationClient;
    }

    public async Task Consume(ConsumeContext<OrgAcceptedTerms> context)
    {
        _dbContext.Relations.Add(new RelationStatusDto(RelationStatus.Pending, context.Message.SubjectId));
        await _dbContext.SaveChangesAsync();

        throw new NotImplementedException();
    }

    public async Task CreateRelation(OrgAcceptedTerms acceptedTerms)
    {
        var request = new CreateRelationRequest
        {
            Subject = descriptor.Subject.ToString(),
            Actor = descriptor.Id.ToString(),
            Ssn = "",
            Tin = descriptor.Organization?.Tin
        };
        try
        {
            var res = await _relationClient.CreateRelationAsync(request, cancellationToken: CancellationToken.None);
            if (res.Success == false)
            {
                logger.LogWarning("AcceptTerms: Unable to create relations for {Subject}. Error: {ErrorMessage}",
                    descriptor.Subject, res.ErrorMessage);
            }
            else
            {
                _dbContext.Relations.Update(new RelationStatusDto(RelationStatus.Created, acceptedTerms.SubjectId));
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError("AcceptTerms: Unable to create relations for {Subject}. Exception: {e}",
                descriptor.Subject, e);
        }
    }
}
