using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using EnergyOrigin.Domain.ValueObjects;

namespace AdminPortal.Utilities.Local;

public class MockAuthorizationService : IAuthorizationService
{
    public Task<GetOrganizationsResponse> GetOrganizationsAsync(CancellationToken cancellationToken)
    {
        var response = new GetOrganizationsResponse(MockData.Organizations);
        return Task.FromResult(response);
    }

    public Task<GetWhitelistedOrganizationsResponse> GetWhitelistedOrganizationsAsync(CancellationToken cancellationToken)
    {
        var response = new GetWhitelistedOrganizationsResponse(MockData.WhitelistedOrganizations);
        return Task.FromResult(response);
    }

    public Task AddOrganizationToWhitelistAsync(Tin tin, CancellationToken cancellationToken)
    {
        var organization = MockData.Organizations.FirstOrDefault(o => o.Tin == tin.Value);

        if (organization == null)
            throw new InvalidOperationException($"Organization with TIN {tin.Value} not found.");

        if (!MockData.WhitelistedOrganizations.Any(o => o.Tin == tin.Value))
        {
            MockData.WhitelistedOrganizations.Add(
                new GetWhitelistedOrganizationsResponseItem(organization.OrganizationId, tin.Value));
        }

        return Task.CompletedTask;
    }

    public Task RemoveOrganizationFromWhitelistAsync(Tin tin, CancellationToken cancellationToken)
    {
        var organizationToRemove = MockData.WhitelistedOrganizations.FirstOrDefault(o => o.Tin == tin.Value);

        if (organizationToRemove != null)
        {
            MockData.WhitelistedOrganizations.Remove(organizationToRemove);
        }

        return Task.CompletedTask;
    }
}
