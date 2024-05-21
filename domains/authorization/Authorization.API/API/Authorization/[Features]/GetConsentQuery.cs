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

        var consent = await clientRepository.Query()
            .Where(client => client.IdpClientId == idpClientId)
            .SelectMany(client => client.Consents.Select(consent =>
                new ConsentDto(client.IdpClientId, consent.Organization.Name, client.RedirectUrl)))
            .ToListAsync(cancellationToken);

        return new GetConsentsQueryResult(consent);
    }
}

public record GetConsentQuery(Guid IdpClientId) : IRequest<GetConsentsQueryResult>;

public record GetConsentsQueryResult(List<ConsentDto> Result);

public record ConsentDto(IdpClientId IdpClientId, OrganizationName OrganizationName, string RedirectUrl);
