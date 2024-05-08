using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetConsentQueryHandler(IConsentRepository consentRepository)
    : IRequestHandler<GetConsentQuery, GetConsentsQueryResult>
{
    public async Task<GetConsentsQueryResult> Handle(GetConsentQuery request, CancellationToken cancellationToken)
    {
        var query = consentRepository.Query();

        var consent = await query
            .Where(it => it.ClientId == request.ClientId)
            .Select(it => new ConsentDto(it.ClientId, it.Organization.Name, it.Client.RedirectUrl))
            .ToListAsync(cancellationToken);

        return new GetConsentsQueryResult(consent);
    }
}

public record GetConsentQuery(Guid ClientId) : IRequest<GetConsentsQueryResult>;

public record GetConsentsQueryResult(List<ConsentDto> Result);

public record ConsentDto(Guid ClientId, OrganizationName OrganizationName, string RedirectUrl);
