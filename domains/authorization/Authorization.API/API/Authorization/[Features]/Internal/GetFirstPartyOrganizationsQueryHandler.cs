using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetFirstPartyOrganizationsQueryHandler(
    IOrganizationRepository organizationRepository
) : IRequestHandler<GetFirstPartyOrganizationsQuery, GetFirstPartyOrganizationsQueryResult>
{
    public async Task<GetFirstPartyOrganizationsQueryResult> Handle(GetFirstPartyOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var firstPartyOrganizations = await organizationRepository.Query()
            .Where(o => o.Tin != Tin.Empty())
            .Select(o => new GetFirstPartyOrganizationsQueryResultItem(
                o.Id,
                o.Name.Value,
                o.Tin!.Value)
            )
            .ToListAsync(cancellationToken);

        return new GetFirstPartyOrganizationsQueryResult(firstPartyOrganizations);
    }
}

public record GetFirstPartyOrganizationsQuery() : IRequest<GetFirstPartyOrganizationsQueryResult>;

public record GetFirstPartyOrganizationsQueryResult(List<GetFirstPartyOrganizationsQueryResultItem> Result);

public record GetFirstPartyOrganizationsQueryResultItem(Guid OrganizationId, string OrganizationName, string Tin);
