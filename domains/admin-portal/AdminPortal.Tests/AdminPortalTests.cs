using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AdminPortal.Dtos;
using AdminPortal.Services;
using AdminPortal.Tests.Setup;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;

namespace AdminPortal.Tests;

public class AdminPortalTests
{
    [Fact]
    public async Task Given_AggregationService_When_Called_Then_ReturnResponseWithActiveMeteringPoints()
    {
        var aggregationService = Substitute.For<IAggregationService>();

        var singleMeteringPoint = new MeteringPoint
        {
            GSRN = "123456789012345678",
            Created = 1625097600,
            StartDate = 1625097600,
            EndDate = 1627689600,
            MeteringPointType = MeteringPointType.Consumption,
            OrganizationName = "Organization1",
            Tin = "TIN123"
        };

        var expectedResponse = new ActiveContractsResponse
        {
            Results = new ResultsData { MeteringPoints = new List<MeteringPoint> { singleMeteringPoint } }
        };

        aggregationService.GetActiveContractsAsync().Returns(Task.FromResult(expectedResponse));

        var result = await aggregationService.GetActiveContractsAsync();

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("123456789012345678", result.Results.MeteringPoints[0].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[0].MeteringPointType);
    }

    [Fact]
    public async Task Given_MatchingResultsFromUpstreamSubsystems_When_GetActiveContractsAsyncIsCalled_Then_ReturnsExpectedResults()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationFacade>();
        var mockCertificatesFacade = Substitute.For<ICertificatesFacade>();
        var organizationId = Guid.NewGuid();
        var organizationName = "Peter Producent A/S";
        var organizationTin = "11223344";

        var predefinedOrganizations = new FirstPartyOrganizationsResponse(new List<FirstPartyOrganizationsResponseItem>
        {
            new(organizationId, organizationName, organizationTin)
        });

        var predefinedContracts = new ContractsForAdminPortalResponse(new List<ContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsAsync().Returns(Task.FromResult(predefinedContracts));

        var service = new ActiveContractsService(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await service.GetActiveContractsAsync();

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("123456789012345678", result.Results.MeteringPoints[0].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[0].MeteringPointType);
        Assert.Equal(organizationName, result.Results.MeteringPoints[0].OrganizationName);
        Assert.Equal(organizationTin, result.Results.MeteringPoints[0].Tin);
    }

    [Fact]
    public async Task Given_UpstreamResultFromAuthorizationContainsAnOrganizationWithNoMatchingContractFromUpstreamCertificates_When_CallingGetActiveContractsAsync_Then_ReturnEmptyResponseInAdminPortal()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationFacade>();
        var mockCertificatesFacade = Substitute.For<ICertificatesFacade>();

        var predefinedOrganizations = new FirstPartyOrganizationsResponse(new List<FirstPartyOrganizationsResponseItem>
        {
            new(Guid.NewGuid(), "Peter Producent A/S", "11223344")
        });

        var predefinedContracts = new ContractsForAdminPortalResponse(new List<ContractsForAdminPortalResponseItem>());

        mockAuthorizationFacade.GetOrganizationsAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsAsync().Returns(Task.FromResult(predefinedContracts));

        var service = new ActiveContractsService(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await service.GetActiveContractsAsync();

        Assert.Empty(result.Results.MeteringPoints);
    }

    [Fact]
    public async Task Given_ResultFromCertificatesContainsAContractWithNoMatchingOrganizationFromAuthorization_When_CallingGetActiveContractsAsync_Then_ReturnEmptyResponseInAdminPortal()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationFacade>();
        var mockCertificatesFacade = Substitute.For<ICertificatesFacade>();

        var predefinedOrganizations = new FirstPartyOrganizationsResponse(new List<FirstPartyOrganizationsResponseItem>());

        var predefinedContracts = new ContractsForAdminPortalResponse(new List<ContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", Guid.NewGuid().ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsAsync().Returns(Task.FromResult(predefinedContracts));

        var service = new ActiveContractsService(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await service.GetActiveContractsAsync();

        Assert.Empty(result.Results.MeteringPoints);
    }

    [Fact]
    public async Task Given_MultipleContractsMatchingSingleOrganization_When_GetActiveContractsAsyncIsCalled_Then_ReturnsListOfMeteringPoints()
    {
        var mockAuthorizationFacade = Substitute.For<IAuthorizationFacade>();
        var mockCertificatesFacade = Substitute.For<ICertificatesFacade>();
        var organizationId = Guid.NewGuid();
        var organizationName = "Peter Producent A/S";
        var organizationTin = "11223344";

        var predefinedOrganizations = new FirstPartyOrganizationsResponse(new List<FirstPartyOrganizationsResponseItem>
        {
            new(organizationId, organizationName, organizationTin)
        });

        var predefinedContracts = new ContractsForAdminPortalResponse(new List<ContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption),
            new("223456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Production),
            new("323456789012345678", organizationId.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsAsync().Returns(Task.FromResult(predefinedContracts));

        var service = new ActiveContractsService(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await service.GetActiveContractsAsync();

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
        var mockAuthorizationFacade = Substitute.For<IAuthorizationFacade>();
        var mockCertificatesFacade = Substitute.For<ICertificatesFacade>();
        var organizationId1 = Guid.NewGuid();
        var organizationId2 = Guid.NewGuid();
        var organizationName1 = "Peter Producent A/S";
        var organizationName2 = "John Distributor A/S";
        var organizationTin1 = "11223344";
        var organizationTin2 = "55667788";

        var predefinedOrganizations = new FirstPartyOrganizationsResponse(new List<FirstPartyOrganizationsResponseItem>
        {
            new(organizationId1, organizationName1, organizationTin1),
            new(organizationId2, organizationName2, organizationTin2)
        });

        var predefinedContracts = new ContractsForAdminPortalResponse(new List<ContractsForAdminPortalResponseItem>
        {
            new("123456789012345678", organizationId1.ToString(), 1625097600, 1625097600, 1627689600, MeteringPointType.Consumption)
        });

        mockAuthorizationFacade.GetOrganizationsAsync().Returns(Task.FromResult(predefinedOrganizations));
        mockCertificatesFacade.GetContractsAsync().Returns(Task.FromResult(predefinedContracts));

        var service = new ActiveContractsService(mockAuthorizationFacade, mockCertificatesFacade);

        var result = await service.GetActiveContractsAsync();

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("123456789012345678", result.Results.MeteringPoints[0].GSRN);
        Assert.Equal(MeteringPointType.Consumption, result.Results.MeteringPoints[0].MeteringPointType);
        Assert.Equal(organizationName1, result.Results.MeteringPoints[0].OrganizationName);
        Assert.Equal(organizationTin1, result.Results.MeteringPoints[0].Tin);
    }

    [Fact]
    public async Task Given_UserIsNotAuthenticated_When_AccessingAdminPortal_Then_Return401Unauthorized()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ett-admin-portal/ActiveContracts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_UserIsAuthenticated_When_AccessingAdminPortal_Then_Return200OK()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var response = await client.GetAsync("/ett-admin-portal/ActiveContracts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Active Contracts", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
