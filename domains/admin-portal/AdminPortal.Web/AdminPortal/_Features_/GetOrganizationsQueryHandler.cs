using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;

public record GetOrganizationsQuery : IRequest<GetOrganizationsQueryResponse>
{
}
public record GetOrganizationsQueryResponse(List<GetOrganizationsQueryResponseItem> Organizations);
public record GetOrganizationsQueryResponseItem(Guid OrganizationId, string OrganizationName, string Tin, string Status);

public class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, GetOrganizationsQueryResponse>
{
    private readonly IAuthorizationService _authorizationService;

    public GetOrganizationsQueryHandler(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<GetOrganizationsQueryResponse> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var response = (await _authorizationService.GetOrganizationsAsync(cancellationToken))
            .Result
            .Select(o => new GetOrganizationsQueryResponseItem(o.OrganizationId, o.OrganizationName, o.Tin, o.Status))
            .ToList();

        return new GetOrganizationsQueryResponse(response);
    }
}
