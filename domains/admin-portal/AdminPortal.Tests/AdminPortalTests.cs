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

        Assert.Contains("12345", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
