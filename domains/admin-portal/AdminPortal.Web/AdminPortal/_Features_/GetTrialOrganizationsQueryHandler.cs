using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;

public record GetTrialOrganizationsQuery : IRequest<GetTrialOrganizationsQueryResponse>
{
}
public record GetTrialOrganizationsQueryResponse(List<GetTrialOrganizationsQueryResponseItem> Organizations);
public record GetTrialOrganizationsQueryResponseItem(Guid OrganizationId, string OrganizationName, string Tin);

public class GetTrialOrganizationsQueryHandler : IRequestHandler<GetTrialOrganizationsQuery, GetTrialOrganizationsQueryResponse>
{
    private readonly IAuthorizationService _authorizationService;

    public GetTrialOrganizationsQueryHandler(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<GetTrialOrganizationsQueryResponse> Handle(GetTrialOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var response = (await _authorizationService.GetOrganizationsAsync(cancellationToken))
            .Result
            .Where(org => org.Status == "Trial")
            .Select(o => new GetTrialOrganizationsQueryResponseItem(o.OrganizationId, o.OrganizationName, o.Tin))
            .ToList();

        return new GetTrialOrganizationsQueryResponse(response);
    }
}
