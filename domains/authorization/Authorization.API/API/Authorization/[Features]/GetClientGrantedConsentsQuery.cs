using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetClientGrantedConsentsQueryHandler(IClientRepository clientRepository) : IRequestHandler<GetClientGrantedConsentsQuery, GetClientGrantedConsentsQueryResult>
{
    public async Task<GetClientGrantedConsentsQueryResult> Handle(GetClientGrantedConsentsQuery request, CancellationToken cancellationToken)
    {
        var queryResultItems = await clientRepository
            .Query()
            .Where(x => x.IdpClientId == request.IdpClientId)
            .SelectMany(x => x.Organization!.OrganizationReceivedConsents)
            .Select(x => new GetClientGrantedConsentsQueryResultItem(x.ConsentGiverOrganization.Id, x.ConsentGiverOrganization.Name, x.ConsentGiverOrganization.Tin))
            .ToListAsync(cancellationToken);

        return new GetClientGrantedConsentsQueryResult(queryResultItems);
    }
}

public record GetClientGrantedConsentsQuery(IdpClientId IdpClientId) : IRequest<GetClientGrantedConsentsQueryResult>;

public record GetClientGrantedConsentsQueryResult(List<GetClientGrantedConsentsQueryResultItem> GetClientConsentsQueryResultItems);

public record GetClientGrantedConsentsQueryResultItem(Guid OrganizationId, OrganizationName OrganizationName, Tin? Tin);
