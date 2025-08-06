using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class GetWhitelistedOrganizationsQueryHandlerTests
{
    [Fact]
    public async Task
        Given_NoWhitelistedOrganizations_When_GetWhitelistedOrganizationsAsyncIsCalled_Then_ReturnsEmptyList()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var mockTransferService = Substitute.For<ITransferService>();
        var organizationId = Guid.NewGuid();
        var organizationTin = "12345678";

        var predefinedOrganizations = new GetWhitelistedOrganizationsResponse(
            new List<GetWhitelistedOrganizationsResponseItem>
            {
                new(organizationId, organizationTin)
            });

        var predefinedWhitelistedOrgs =
            new GetWhitelistedOrganizationsResponse(new List<GetWhitelistedOrganizationsResponseItem>());

        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(predefinedWhitelistedOrgs));

        var handler = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService, mockTransferService);

        var result = await handler.Handle(new GetWhitelistedOrganizationsQuery(), CancellationToken.None);

        Assert.Empty(result.ViewModel);
    }

    [Fact]
    public async Task
        Given_MultipleWhitelistedOrganizations_When_NoCompanyInformation_Then_ReturnsAllMatchingOrganizationsWithEmptyCompanyNames()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var mockTransferService = Substitute.For<ITransferService>();
        var organizationId1 = Guid.NewGuid();
        var organizationId2 = Guid.NewGuid();
        var organizationTin1 = "12345678";
        var organizationTin2 = "87654321";

        var predefinedOrganizations = new GetWhitelistedOrganizationsResponse(
            new List<GetWhitelistedOrganizationsResponseItem>
            {
                new(organizationId1, organizationTin1),
                new(organizationId2, organizationTin2)
            });

        var predefinedWhitelistedOrgs = new GetWhitelistedOrganizationsResponse(
            new List<GetWhitelistedOrganizationsResponseItem>
            {
                new(organizationId1, organizationTin1),
                new(organizationId2, organizationTin2)
            });

        var orgs = new GetOrganizationsResponse([
            new GetOrganizationsResponseItem(organizationId1, "Org 1", organizationTin1, "normal"),
            new GetOrganizationsResponseItem(organizationId2, "Org 2", organizationTin2, "trial")
        ]);

        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(orgs);
        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(predefinedWhitelistedOrgs));

        mockTransferService.GetCompanies(Arg.Any<List<string>>())
            .Returns(Task.FromResult(new CvrCompaniesListResponse
            {
                Result = []
            }));

        var handler = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService, mockTransferService);

        var result = await handler.Handle(new GetWhitelistedOrganizationsQuery(), CancellationToken.None);

        Assert.Equal(2, result.ViewModel.Count);

        Assert.Equal(organizationId1, result.ViewModel[0].OrganizationId);
        Assert.Equal(organizationTin1, result.ViewModel[0].Tin);
        Assert.Equal(string.Empty, result.ViewModel[0].CompanyName);
        Assert.Equal("normal", result.ViewModel[0].Status);

        Assert.Equal(organizationId2, result.ViewModel[1].OrganizationId);
        Assert.Equal(organizationTin2, result.ViewModel[1].Tin);
        Assert.Equal(string.Empty, result.ViewModel[1].CompanyName);
        Assert.Equal("trial", result.ViewModel[1].Status);
    }

    [Fact]
    public async Task
        Given_MultipleWhitelistedOrganizations_When_GetWhitelistedOrganizationsAsyncIsCalled_Then_ReturnsAllMatchingOrganizations()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var mockTransferService = Substitute.For<ITransferService>();
        var organizationId1 = Guid.NewGuid();
        var organizationId2 = Guid.NewGuid();
        var organizationTin1 = "12345678";
        var organizationTin2 = "87654321";
        var organizationName1 = "Org 1";
        var organizationName2 = "Org 2";

        var predefinedOrganizations = new GetWhitelistedOrganizationsResponse(
            new List<GetWhitelistedOrganizationsResponseItem>
            {
                new(organizationId1, organizationTin1),
                new(organizationId2, organizationTin2)
            });

        var predefinedWhitelistedOrgs = new GetWhitelistedOrganizationsResponse(
            new List<GetWhitelistedOrganizationsResponseItem>
            {
                new(organizationId1, organizationTin1),
                new(organizationId2, organizationTin2)
            });

        var orgs = new GetOrganizationsResponse([
            new GetOrganizationsResponseItem(organizationId1, "Org 1", organizationTin1, "normal"),
            new GetOrganizationsResponseItem(organizationId2, "Org 2", organizationTin2, "trial")
        ]);

        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(orgs);

        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(predefinedOrganizations));
        mockAuthorizationService.GetWhitelistedOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(predefinedWhitelistedOrgs));

        mockTransferService.GetCompanies(Arg.Any<List<string>>())
            .Returns(Task.FromResult(new CvrCompaniesListResponse
            {
                Result =
                [
                    new CvrCompaniesInformationDto
                    {
                        Name = organizationName1,
                        Tin = organizationTin1
                    },
                    new CvrCompaniesInformationDto
                    {
                        Name = organizationName2,
                        Tin = organizationTin2
                    }
                ]
            }));

        var handler = new GetWhitelistedOrganizationsQueryHandler(mockAuthorizationService, mockTransferService);

        var result = await handler.Handle(new GetWhitelistedOrganizationsQuery(), CancellationToken.None);

        Assert.Equal(2, result.ViewModel.Count);

        Assert.Equal(organizationId1, result.ViewModel[0].OrganizationId);
        Assert.Equal(organizationTin1, result.ViewModel[0].Tin);
        Assert.Equal(organizationName1, result.ViewModel[0].CompanyName);
        Assert.Equal("normal", result.ViewModel[0].Status);

        Assert.Equal(organizationId2, result.ViewModel[1].OrganizationId);
        Assert.Equal(organizationTin2, result.ViewModel[1].Tin);
        Assert.Equal(organizationName2, result.ViewModel[1].CompanyName);
        Assert.Equal("trial", result.ViewModel[1].Status);
    }
}
