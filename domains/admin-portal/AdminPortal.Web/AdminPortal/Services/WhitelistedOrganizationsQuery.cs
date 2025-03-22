using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;

namespace AdminPortal.Services;

public interface IWhitelistedOrganizationsQuery
{
    Task<List<WhitelistedOrganizationViewModel>> GetWhitelistedOrganizationsAsync();
}

public class WhitelistedOrganizationsQuery : IWhitelistedOrganizationsQuery
{
    private readonly IAuthorizationService _authorizationService;

    public WhitelistedOrganizationsQuery(IAuthorizationService authorizationService)
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
