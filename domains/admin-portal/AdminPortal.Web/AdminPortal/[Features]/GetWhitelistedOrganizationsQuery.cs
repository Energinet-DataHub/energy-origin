using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;

namespace AdminPortal._Features_;

public interface IWhitelistedOrganizationsQuery
{
    Task<List<WhitelistedOrganizationViewModel>> GetWhitelistedOrganizationsAsync();
}

public class GetWhitelistedOrganizationsQuery : IWhitelistedOrganizationsQuery
{
    private readonly IAuthorizationService _authorizationService;

    public GetWhitelistedOrganizationsQuery(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<List<WhitelistedOrganizationViewModel>> GetWhitelistedOrganizationsAsync()
    {
        var whitelistedOrganizations = await _authorizationService.GetWhitelistedOrganizationsAsync();

        var result = whitelistedOrganizations.Result
            .Select(whitelisted => new WhitelistedOrganizationViewModel
            {
                OrganizationId = whitelisted.OrganizationId,
                Tin = whitelisted.Tin
            })
            .ToList();

        return result;
    }
}
