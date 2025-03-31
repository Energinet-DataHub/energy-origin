using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class GetWhitelistedOrganizationsQueryHandlerTests
{
    [Fact]
    public async Task Given_NoWhitelistedOrganizations_When_GetWhitelistedOrganizationsAsyncIsCalled_Then_ReturnsEmptyList()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var organizationId = Guid.NewGuid();
        var organizationTin = "12345678";

        var predefinedOrganizations = new GetWhitelistedOrganizationsResponse(new List<GetWhitelistedOrganizationsResponseItem>
        {
            new(organizationId, organizationTin)
        });

        var predefinedWhitelistedOrgs = new GetWhitelistedOrganizationsResponse(new List<GetWhitelistedOrganizationsResponseItem>());

        mockAuthorizationService.GetWhitelistedOrganizationsHttpRequestAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsHttpRequestAsync().Returns(Task.FromResult(predefinedWhitelistedOrgs));

        var service = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService);

        var result = await service.GetWhitelistedOrganizationsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task Given_MultipleWhitelistedOrganizations_When_GetWhitelistedOrganizationsAsyncIsCalled_Then_ReturnsAllMatchingOrganizations()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var organizationId1 = Guid.NewGuid();
        var organizationId2 = Guid.NewGuid();
        var organizationTin1 = "12345678";
        var organizationTin2 = "87654321";

        var predefinedOrganizations = new GetWhitelistedOrganizationsResponse(new List<GetWhitelistedOrganizationsResponseItem>
        {
            new(organizationId1, organizationTin1),
            new(organizationId2, organizationTin2)
        });

        var predefinedWhitelistedOrgs = new GetWhitelistedOrganizationsResponse(new List<GetWhitelistedOrganizationsResponseItem>
        {
            new(organizationId1, organizationTin1),
            new(organizationId2, organizationTin2)
        });

        mockAuthorizationService.GetWhitelistedOrganizationsHttpRequestAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsHttpRequestAsync().Returns(Task.FromResult(predefinedWhitelistedOrgs));

        var service = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService);

        var result = await service.GetWhitelistedOrganizationsAsync();

        Assert.Equal(2, result.Count);

        Assert.Equal(organizationId1, result[0].OrganizationId);
        Assert.Equal(organizationTin1, result[0].Tin);

        Assert.Equal(organizationId2, result[1].OrganizationId);
        Assert.Equal(organizationTin2, result[1].Tin);
    }
}
