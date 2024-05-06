using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetConsentQueryHandler(IConsentRepository consentRepository)
    : IRequestHandler<GetConsentQuery, GetConsentQueryResult>
{
    public async Task<GetConsentQueryResult> Handle(GetConsentQuery request, CancellationToken cancellationToken)
    {
        var result = await
            consentRepository.Query().Where(it => it.ClientId == request.ClientId).Select(it =>
                    new GetConsentQueryResult(it.ClientId, it.Organization.OrganizationName, it.Client.RedirectUrl))
                .FirstAsync(cancellationToken);
        if (result is null)
        {
            throw new EntityNotFoundException(request.ClientId, nameof(GetConsentQueryResult));
        }

        return result;
    }
}

public record GetConsentQuery(Guid ClientId) : IRequest<GetConsentQueryResult>;

public record GetConsentQueryResult(Guid ClientId, OrganizationName Name, string RedirectUrl);
