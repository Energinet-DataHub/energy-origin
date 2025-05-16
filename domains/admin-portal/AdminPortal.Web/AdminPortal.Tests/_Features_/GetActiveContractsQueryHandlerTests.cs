using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class GetActiveContractsQueryHandlerTests
{
    [Fact]
    public async Task Given_MatchingResultsFromUpstreamSubsystems_When_GetActiveContractsAsyncIsCalled_Then_ReturnsExpectedResults()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationService>();
        var mockCertificatesFacade = Substitute.For<ICertificatesService>();
        var organizationId = Guid.NewGuid();
        var organizationName = "Peter Producent A/S";
        var organizationTin = "11223344";

        var predefinedOrganizations = new GetOrganizationsResponse(new List<GetOrganizationsResponseItem>
        {
            new(organizationId, organizationName, organizationTin)
        });

        var predefinedContracts = new GetContractsForAdminPortalResponse(new List<GetContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsHttpRequestAsync().Returns(Task.FromResult(predefinedContracts));

        var handler = new GetActiveContractsQueryHandler(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await handler.Handle(new GetActiveContractsQuery(), CancellationToken.None);

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("123456789012345678", result.Results.MeteringPoints[0].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[0].MeteringPointType);
        Assert.Equal(organizationName, result.Results.MeteringPoints[0].OrganizationName);
        Assert.Equal(organizationTin, result.Results.MeteringPoints[0].Tin);
    }

    [Fact]
    public async Task Given_UpstreamResultFromAuthorizationContainsAnOrganizationWithNoMatchingContractFromUpstreamCertificates_When_CallingGetActiveContractsAsync_Then_ReturnEmptyResponseInAdminPortal()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationService>();
        var mockCertificatesFacade = Substitute.For<ICertificatesService>();

        var predefinedOrganizations = new GetOrganizationsResponse(new List<GetOrganizationsResponseItem>
        {
            new(Guid.NewGuid(), "Peter Producent A/S", "11223344")
        });

        var predefinedContracts = new GetContractsForAdminPortalResponse(new List<GetContractsForAdminPortalResponseItem>());

        mockAuthorizationFacade.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsHttpRequestAsync().Returns(Task.FromResult(predefinedContracts));

        var handler = new GetActiveContractsQueryHandler(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await handler.Handle(new GetActiveContractsQuery(), CancellationToken.None);

        Assert.Empty(result.Results.MeteringPoints);
    }

    [Fact]
    public async Task Given_ResultFromCertificatesContainsAContractWithNoMatchingOrganizationFromAuthorization_When_CallingGetActiveContractsAsync_Then_ReturnEmptyResponseInAdminPortal()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationService>();
        var mockCertificatesFacade = Substitute.For<ICertificatesService>();

        var predefinedOrganizations = new GetOrganizationsResponse(new List<GetOrganizationsResponseItem>());

        var predefinedContracts = new GetContractsForAdminPortalResponse(new List<GetContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", Guid.NewGuid().ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsHttpRequestAsync().Returns(Task.FromResult(predefinedContracts));

        var handler = new GetActiveContractsQueryHandler(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await handler.Handle(new GetActiveContractsQuery(), CancellationToken.None);

        Assert.Empty(result.Results.MeteringPoints);
    }

    [Fact]
    public async Task Given_MultipleContractsMatchingSingleOrganization_When_GetActiveContractsAsyncIsCalled_Then_ReturnsListOfMeteringPoints()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationService>();
        var mockCertificatesFacade = Substitute.For<ICertificatesService>();
        var organizationId = Guid.NewGuid();
        var organizationName = "Peter Producent A/S";
        var organizationTin = "11223344";

        var predefinedOrganizations = new GetOrganizationsResponse(new List<GetOrganizationsResponseItem>
        {
            new(organizationId, organizationName, organizationTin)
        });

        var predefinedContracts = new GetContractsForAdminPortalResponse(new List<GetContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption),
            new("223456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Production),
            new("323456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsHttpRequestAsync().Returns(Task.FromResult(predefinedContracts));

        var handler = new GetActiveContractsQueryHandler(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await handler.Handle(new GetActiveContractsQuery(), CancellationToken.None);

        Assert.Equal(3, result.Results.MeteringPoints.Count);
        Assert.Equal("123456789012345678", result.Results.MeteringPoints[0].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[0].MeteringPointType);
        Assert.Equal(organizationName, result.Results.MeteringPoints[0].OrganizationName);
        Assert.Equal(organizationTin, result.Results.MeteringPoints[0].Tin);

        Assert.Equal("223456789012345678", result.Results.MeteringPoints[1].GSRN);
        Assert.Equal(MeteringPointType.Production, result.Results.MeteringPoints[1].MeteringPointType);
        Assert.Equal(organizationName, result.Results.MeteringPoints[1].OrganizationName);
        Assert.Equal(organizationTin, result.Results.MeteringPoints[1].Tin);

        Assert.Equal("323456789012345678", result.Results.MeteringPoints[2].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[2].MeteringPointType);
        Assert.Equal(organizationName, result.Results.MeteringPoints[2].OrganizationName);
        Assert.Equal(organizationTin, result.Results.MeteringPoints[2].Tin);
    }

    [Fact]
    public async Task Given_OneContractAndTwoOrganizations_When_CallingGetActiveContractsAsync_Then_ReturnResponseWithMatchingMeteringPointsOnly()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationService>();
        var mockCertificatesFacade = Substitute.For<ICertificatesService>();
        var organizationId1 = Guid.NewGuid();
        var organizationId2 = Guid.NewGuid();
        var organizationName1 = "Peter Producent A/S";
        var organizationName2 = "John Distributor A/S";
        var organizationTin1 = "11223344";
        var organizationTin2 = "55667788";

        var predefinedOrganizations = new GetOrganizationsResponse(new List<GetOrganizationsResponseItem>
        {
            new(organizationId1, organizationName1, organizationTin1),
            new(organizationId2, organizationName2, organizationTin2)
        });

        var predefinedContracts = new GetContractsForAdminPortalResponse(new List<GetContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", organizationId1.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsHttpRequestAsync().Returns(Task.FromResult(predefinedContracts));

        var handler = new GetActiveContractsQueryHandler(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await handler.Handle(new GetActiveContractsQuery(), CancellationToken.None);

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("123456789012345678", result.Results.MeteringPoints[0].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[0].MeteringPointType);
        Assert.Equal(organizationName1, result.Results.MeteringPoints[0].OrganizationName);
        Assert.Equal(organizationTin1, result.Results.MeteringPoints[0].Tin);
    }
}
