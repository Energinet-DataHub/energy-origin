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

public class GetConsentQueryHandler(IClientRepository clientRepository)
    : IRequestHandler<GetConsentQuery, GetConsentsQueryResult>
{
    public async Task<GetConsentsQueryResult> Handle(GetConsentQuery request, CancellationToken cancellationToken)
    {
        var idpClientId = new IdpClientId(request.IdpClientId);

        var consents = await clientRepository
            .Query()
            .Where(client => client.IdpClientId == idpClientId)
            .SelectMany(x => x.Organization!.OrganizationGivenConsents.Select(y => new GetConsentQueryResultItem(x.IdpClientId, x.Organization.Name, x.RedirectUrl)))
            .ToListAsync();

        return new GetConsentsQueryResult(consents);
    }
}

public record GetConsentQuery(Guid IdpClientId) : IRequest<GetConsentsQueryResult>;

public record GetConsentsQueryResult(List<GetConsentQueryResultItem> Result);

public record GetConsentQueryResultItem(IdpClientId IdpClientId, OrganizationName OrganizationName, string RedirectUrl);
