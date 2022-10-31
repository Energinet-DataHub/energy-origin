using System.Numerics;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Issuer.UnitTests.GranularCertificateIssuer;

public class EnergyMeasuredEventHandlerTest
{
    private readonly MasterData validMasterData = new(
        "gsrn",
        "gridArea",
        MeteringPointType.Production,
        new Technology("F00000000", "T010000"),
        "meteringPointOwner",
        true);

    [Fact]
    public async Task Handle_NoMasterData_NoEvent()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData("gsrn"))
            .ReturnsAsync(null as MasterData);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object, Mock.Of<ILogger<EnergyMeasuredEventHandler>>());

        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await handler.Handle(@event);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Handle_ConsumptionPoint_NoEvent()
    {
        var masterDataForConsumptionPoint = validMasterData with { Type = MeteringPointType.Consumption };

        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock
            .Setup(m => m.GetMasterData(masterDataForConsumptionPoint.GSRN))
            .ReturnsAsync(masterDataForConsumptionPoint);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object, Mock.Of<ILogger<EnergyMeasuredEventHandler>>());

        var @event = new EnergyMeasured(masterDataForConsumptionPoint.GSRN, new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await handler.Handle(@event);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Handle_MeteringPointNotOnboarded_NoEvent()
    {
        var masterDataForNotOnboarded = validMasterData with { MeteringPointOnboarded = false };

        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock
            .Setup(m => m.GetMasterData(masterDataForNotOnboarded.GSRN))
            .ReturnsAsync(masterDataForNotOnboarded);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object, Mock.Of<ILogger<EnergyMeasuredEventHandler>>());

        var @event = new EnergyMeasured(masterDataForNotOnboarded.GSRN, new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await handler.Handle(@event);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Handle_ProductionPoint_ProducesAnEvent()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock
            .Setup(m => m.GetMasterData(validMasterData.GSRN))
            .ReturnsAsync(validMasterData);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object, Mock.Of<ILogger<EnergyMeasuredEventHandler>>());

        var @event = new EnergyMeasured(validMasterData.GSRN, new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await handler.Handle(@event);

        Assert.NotNull(producedEvent);

        var expected = new ProductionCertificateCreated(
            producedEvent!.CertificateId,
            validMasterData.GridArea,
            @event.Period,
            validMasterData.Technology,
            validMasterData.MeteringPointOwner,
            new ShieldedValue<string>(@event.GSRN, BigInteger.Zero),
            new ShieldedValue<long>(@event.Quantity, BigInteger.Zero));

        Assert.Equal(expected, producedEvent);
    }
}
