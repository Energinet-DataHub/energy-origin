using System.Numerics;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
using Moq;
using Xunit;

namespace Issuer.UnitTests.GranularCertificateIssuer;

public class EnergyMeasuredEventHandlerTest
{
    private readonly MasterData validMasterData = new(
        GSRN: "gsrn",
        GridArea: "gridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointOnboarded: true);

    [Fact]
    public async Task Handle_NoMasterData_NoEvent()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData("gsrn"))
            .ReturnsAsync(value: null);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object);

        var @event = new EnergyMeasured(
            GSRN: "gsrn",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

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

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object);

        var @event = new EnergyMeasured(
            GSRN: masterDataForConsumptionPoint.GSRN,
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

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
            .ReturnsAsync(value: masterDataForNotOnboarded);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object);

        var @event = new EnergyMeasured(
            GSRN: masterDataForNotOnboarded.GSRN,
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        var producedEvent = await handler.Handle(@event);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Handle_ProductionPoint_ProducesAnEvent()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock
            .Setup(m => m.GetMasterData(validMasterData.GSRN))
            .ReturnsAsync(value: validMasterData);

        var handler = new EnergyMeasuredEventHandler(masterDataServiceMock.Object);

        var @event = new EnergyMeasured(
            GSRN: validMasterData.GSRN,
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        var producedEvent = await handler.Handle(@event);

        Assert.NotNull(producedEvent);

        var expected = new ProductionCertificateCreated(
            CertificateId: producedEvent!.CertificateId,
            GridArea: validMasterData.GridArea,
            Period: @event.Period,
            Technology: validMasterData.Technology,
            MeteringPointOwner: validMasterData.MeteringPointOwner,
            ShieldedGSRN: new ShieldedValue<string>(Value: @event.GSRN, R: BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(Value: @event.Quantity, R: BigInteger.Zero));

        Assert.Equal(expected, producedEvent);
    }
}
