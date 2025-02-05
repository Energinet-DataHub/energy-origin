using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record GetOrganizationsQueryRequest() : IRequest<GetOrganizationsQueryResponse>;

public record GetOrganizationsQueryResponse(List<GetOrganizationsQueryResult> Result);

public record GetOrganizationsQueryResult(Guid OrganizationId, string OrganizationName, string Tin);

public class GetOrganizationsQueryHandler(
    IOrganizationRepository organizationRepository
) : IRequestHandler<GetOrganizationsQueryRequest, GetOrganizationsQueryResponse>
{
    public async Task<GetOrganizationsQueryResponse> Handle(GetOrganizationsQueryRequest request, CancellationToken cancellationToken)
    {
        var organizations = await organizationRepository.Query().ToListAsync(cancellationToken);

        var result = organizations
            .Select(o => new GetOrganizationsQueryResult(
                o.Id,
                o.Name.Value,
                o.Tin?.Value ?? string.Empty
            ))
            .ToList();

        return new GetOrganizationsQueryResponse(result);
    }
}




