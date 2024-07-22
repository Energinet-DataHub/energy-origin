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

public class GetClientConsentsQueryHandler(IOrganizationRepository organizationRepository) : IRequestHandler<GetClientConsentsQuery, GetClientConsentsQueryResult>
{
    public async Task<GetClientConsentsQueryResult> Handle(GetClientConsentsQuery request, CancellationToken cancellationToken)
    {
        var consentsQueryResultItems = await organizationRepository
            .Query()
            .Where(organization => organization.Consents.Any(consent => consent.Client.IdpClientId == request.IdpClientId))
            .Select(x => new GetClientConsentsQueryResultItem(x.Id, x.Name, x.Tin))
            .ToListAsync(cancellationToken);

        return new GetClientConsentsQueryResult(consentsQueryResultItems);
    }
}

public record GetClientConsentsQuery(IdpClientId IdpClientId) : IRequest<GetClientConsentsQueryResult>;

public record GetClientConsentsQueryResult(List<GetClientConsentsQueryResultItem> GetClientConsentsQueryResultItems);

public record GetClientConsentsQueryResultItem(Guid OrganizationId, OrganizationName OrganizationName, Tin Tin);
