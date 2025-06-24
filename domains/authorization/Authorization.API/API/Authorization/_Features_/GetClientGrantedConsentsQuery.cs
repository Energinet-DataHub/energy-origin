using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetClientGrantedConsentsQueryHandler(IClientRepository clientRepository) : IRequestHandler<GetClientGrantedConsentsQuery, GetClientGrantedConsentsQueryResult>
{
    public async Task<GetClientGrantedConsentsQueryResult> Handle(GetClientGrantedConsentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: CABOL - We need to handle internal and external clients. Only external clients can be trial or non trial. It does not make sense for internal clients
        // TODO: CABOL - Change database layout or do something different?

        var isTrial = await clientRepository
            .Query()
            .Where(x => x.IdpClientId == request.IdpClientId)
            .Select(x => (bool?)x.IsTrial)
            .FirstOrDefaultAsync(cancellationToken);

        if (isTrial is null)
        {
            return new GetClientGrantedConsentsQueryResult([]);
        }

        if ((bool)isTrial)
        {
            var trialConsents = await clientRepository.Query()
                .Where(x => x.IdpClientId == request.IdpClientId)
                .SelectMany(x => x.Organization!.OrganizationReceivedConsents)
                .Where(consent => consent.ConsentGiverOrganization.Status == OrganizationStatus.Trial)
                .Select(x => new GetClientGrantedConsentsQueryResultItem(x.ConsentGiverOrganization.Id, x.ConsentGiverOrganization.Name, x.ConsentGiverOrganization.Tin))
                .ToListAsync(cancellationToken);

            return new GetClientGrantedConsentsQueryResult(trialConsents);
        }
        else
        {
            var normalConsents = await clientRepository.Query()
                .Where(x => x.IdpClientId == request.IdpClientId)
                .SelectMany(x => x.Organization!.OrganizationReceivedConsents)
                .Where(consent => consent.ConsentGiverOrganization.Status == OrganizationStatus.Normal)
                .Select(x => new GetClientGrantedConsentsQueryResultItem(x.ConsentGiverOrganization.Id, x.ConsentGiverOrganization.Name, x.ConsentGiverOrganization.Tin))
                .ToListAsync(cancellationToken);

            return new GetClientGrantedConsentsQueryResult(normalConsents);
        }
    }
}

public record GetClientGrantedConsentsQuery(IdpClientId IdpClientId) : IRequest<GetClientGrantedConsentsQueryResult>;

public record GetClientGrantedConsentsQueryResult(List<GetClientGrantedConsentsQueryResultItem> GetClientConsentsQueryResultItems);

public record GetClientGrantedConsentsQueryResultItem(Guid OrganizationId, OrganizationName OrganizationName, Tin? Tin);
