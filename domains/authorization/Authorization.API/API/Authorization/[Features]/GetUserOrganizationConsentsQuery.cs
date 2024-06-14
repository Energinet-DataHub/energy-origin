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

public class GetUserOrganizationConsentsQueryHandler(IClientRepository clientRepository)
    : IRequestHandler<GetUserOrganizationConsentsQuery, GetUserOrganizationConsentsQueryResult>
{
    public async Task<GetUserOrganizationConsentsQueryResult> Handle(GetUserOrganizationConsentsQuery request, CancellationToken cancellationToken)
    {
        var idpUserId = IdpUserId.Create(Guid.Parse(request.IdpUserId));

        var clients = await clientRepository
            .Query()
            .Where(client => client.Consents
                .Any(consent => consent.Organization.Affiliations
                    .Any(o => o.User.IdpUserId == idpUserId)))
            .Select(client => new GetUserOrganizationConsentsQueryResultItem(client.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);

        return new GetUserOrganizationConsentsQueryResult(clients);
    }
}

public record GetUserOrganizationConsentsQuery(string IdpUserId) : IRequest<GetUserOrganizationConsentsQueryResult>;

public record GetUserOrganizationConsentsQueryResult(List<GetUserOrganizationConsentsQueryResultItem> Result);

public record GetUserOrganizationConsentsQueryResultItem(string Name);
