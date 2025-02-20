using System.Collections.Generic;
using System.Threading.Tasks;
using AdminPortal.Dtos;
using AdminPortal.Services;
using NSubstitute;

namespace AdminPortal.Tests;

public class AggregationServiceTests
{
    [Fact]
    public async Task GetActiveContractsAsync_ReturnsSingleEntry()
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
}
