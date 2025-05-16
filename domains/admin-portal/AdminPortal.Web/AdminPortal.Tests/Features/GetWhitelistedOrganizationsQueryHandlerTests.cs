using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Features;
using AdminPortal.Services;
using NSubstitute;

namespace AdminPortal.Tests.Features;

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

        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedWhitelistedOrgs));

        var handler = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService);

        var result = await handler.Handle(new GetWhitelistedOrganizationsQuery(), CancellationToken.None);

        Assert.Empty(result.ViewModel);
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

        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedWhitelistedOrgs));

        var handler = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService);

        var result = await handler.Handle(new GetWhitelistedOrganizationsQuery(), CancellationToken.None);

        Assert.Equal(2, result.ViewModel.Count);

        Assert.Equal(organizationId1, result.ViewModel[0].OrganizationId);
        Assert.Equal(organizationTin1, result.ViewModel[0].Tin);

        Assert.Equal(organizationId2, result.ViewModel[1].OrganizationId);
        Assert.Equal(organizationTin2, result.ViewModel[1].Tin);
    }
}
