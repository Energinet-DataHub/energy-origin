using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;

public class GetWhitelistedOrganizationsQuery : IRequest<WhitelistedOrganizationsQueryResult>
{
}

public class WhitelistedOrganizationsQueryResult(List<WhitelistedOrganizationViewModel> viewModel)
{
    public List<WhitelistedOrganizationViewModel> ViewModel { get; } = viewModel;
}

public class GetWhitelistedOrganizationsQueryHandler(IAuthorizationService authorizationService, ITransferService transferService)
    : IRequestHandler<GetWhitelistedOrganizationsQuery, WhitelistedOrganizationsQueryResult>
{
    public async Task<WhitelistedOrganizationsQueryResult> Handle(GetWhitelistedOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var whitelistedOrganizations = await authorizationService.GetWhitelistedOrganizationsAsync(cancellationToken);

        if (whitelistedOrganizations is null || !whitelistedOrganizations.Result.Any())
        {
            return new WhitelistedOrganizationsQueryResult([]);
        }

        var companies = await transferService.GetCompanies([.. whitelistedOrganizations.Result.Select(w => w.Tin)]);

        var result = whitelistedOrganizations.Result
            .GroupJoin(
                companies.Result,
                whiteListed => whiteListed.Tin,
                company => company.Tin,
                (whiteListed, companyGroup) => new { whiteListed, companyGroup }
            )
            .SelectMany(
                x => x.companyGroup.DefaultIfEmpty(),
                (x, company) => new WhitelistedOrganizationViewModel
                {
                    OrganizationId = x.whiteListed.OrganizationId,
                    CompanyName = company?.Name ?? string.Empty,
                    Tin = x.whiteListed.Tin
                }
            )
            .ToList();

        return new WhitelistedOrganizationsQueryResult(result);
    }
}
