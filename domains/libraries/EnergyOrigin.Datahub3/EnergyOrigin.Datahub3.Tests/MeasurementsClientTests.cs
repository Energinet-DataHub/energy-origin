using Energinet.DataHub.Measurements.Abstractions.Api.Queries;
using EnergyTrackAndTrace.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EnergyOrigin.Datahub3.Tests;

public class MeasurementsClientTests
{
    [Fact]
    public async Task GetMeasurements_WhenInDemoEnvironmentAnyApiReturns1YearOfData_ExpectDataCorrectlyFiltered()
    {
        var mockClient = Substitute.For<Energinet.DataHub.Measurements.Client.IMeasurementsClient>();
        var dh3Options = new DataHub3Options { EnableMock = true };
        IOptions<DataHub3Options> someOptions = Options.Create<DataHub3Options>(dh3Options);

        var gsrn = Any.Gsrn();
        var dateTo = DateTimeOffset.Now;
        var dateFrom = dateTo.AddYears(-1);
        var data = Any.MeasurementsApiResponse(gsrn, dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), 1);

        mockClient.GetAggregatedByPeriodAsync(Arg.Any<GetAggregateByPeriodQuery>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var sut = new MeasurementClient(mockClient, someOptions, Substitute.For<ILogger<MeasurementClient>>());

        var queryDateTo = dateTo.AddDays(-30);
        var queryDateFrom = dateTo.AddDays(-60);
        var response = await sut.GetMeasurements([gsrn], queryDateFrom.ToUnixTimeSeconds(), queryDateTo.ToUnixTimeSeconds(), new CancellationToken());

        Assert.NotNull(response);
        Assert.Equal(30, response.First().PointAggregationGroups.Count);
    }
}
