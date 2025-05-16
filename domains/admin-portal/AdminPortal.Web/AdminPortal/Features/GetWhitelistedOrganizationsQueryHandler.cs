using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal.Features;

public class GetWhitelistedOrganizationsQuery : IRequest<WhitelistedOrganizationsQueryResult>
{
}

public class WhitelistedOrganizationsQueryResult(List<WhitelistedOrganizationViewModel> viewModel)
{
    public List<WhitelistedOrganizationViewModel> ViewModel { get; } = viewModel;
}

public class GetWhitelistedOrganizationsQueryHandler : IRequestHandler<GetWhitelistedOrganizationsQuery, WhitelistedOrganizationsQueryResult>
{
    private readonly IAuthorizationService _authorizationService;

    public GetWhitelistedOrganizationsQueryHandler(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<WhitelistedOrganizationsQueryResult> Handle(GetWhitelistedOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var whitelistedOrganizations = await _authorizationService.GetWhitelistedOrganizationsAsync(cancellationToken);

        var result = whitelistedOrganizations.Result
            .Select(whitelisted => new WhitelistedOrganizationViewModel
            {
                OrganizationId = whitelisted.OrganizationId,
                Tin = whitelisted.Tin
            })
            .ToList();

        return new WhitelistedOrganizationsQueryResult(result);
    }
}
