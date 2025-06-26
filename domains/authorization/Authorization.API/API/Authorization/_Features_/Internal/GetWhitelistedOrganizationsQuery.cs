using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetWhitelistedOrganizationsQueryHandler(
    IWhitelistedRepository whitelistedRepository
) : IRequestHandler<GetWhitelistedOrganizationsQuery, GetWhitelistedOrganizationsQueryResult>
{
    public async Task<GetWhitelistedOrganizationsQueryResult> Handle(GetWhitelistedOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var whitelistedOrganizations = await whitelistedRepository.Query()
            .Select(w => new GetWhitelistedOrganizationsQueryResultItem(
                w.Id,
                w.Tin.Value
            ))
            .ToListAsync(cancellationToken);

        return new GetWhitelistedOrganizationsQueryResult(whitelistedOrganizations);
    }
}

public record GetWhitelistedOrganizationsQuery() : IRequest<GetWhitelistedOrganizationsQueryResult>;

public record GetWhitelistedOrganizationsQueryResult(List<GetWhitelistedOrganizationsQueryResultItem> Result);

public record GetWhitelistedOrganizationsQueryResultItem(Guid OrganizationId, string Tin);
