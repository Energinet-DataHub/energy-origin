using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyTrackAndTrace.Testing.Extensions;
using Meteringpoint.V1;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class ReportValidationQueryHandlerTests
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClientMock;

    public ReportValidationQueryHandlerTests()
    {
        _meteringPointClientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
    }

    [Fact]
    public async Task GivenReportValidation_WhenNoMeteringPoints_ReturnsFalse()
    {
        // Arrange
        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints = { }
            });

        var queryHandler = new ReportValidationQueryHandler(_meteringPointClientMock);

        // Act
        var result = await queryHandler.Handle(new ReportValidationQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.False(result.Valid);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task GivenReportValidation_WhenNoConsumptionMeteringPoints_ReturnsFalse()
    {
        // Arrange
        var gsrn = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ProductionMeteringPoint(gsrn)
                }
            });

        var queryHandler = new ReportValidationQueryHandler(_meteringPointClientMock);

        // Act
        var result = await queryHandler.Handle(new ReportValidationQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.False(result.Valid);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task GivenReportValidation_WhenConsumptionMeteringPoints_ReturnsTrue()
    {
        // Arrange
        var gsrn = Any.Gsrn();

        _meteringPointClientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    EnergyTrackAndTrace.Testing.Any.ConsumptionMeteringPoint(gsrn)
                }
            });

        var queryHandler = new ReportValidationQueryHandler(_meteringPointClientMock);

        // Act
        var result = await queryHandler.Handle(new ReportValidationQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.True(result.Valid);
        Assert.True(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }
}
