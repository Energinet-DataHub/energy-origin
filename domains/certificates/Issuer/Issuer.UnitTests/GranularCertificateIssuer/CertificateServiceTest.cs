using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Issuer.UnitTests.GranularCertificateIssuer;

public class CertificateServiceTest
{
    private readonly MasterData validMasterData = new(
        "gsrn",
        "gridArea",
        MeteringPointType.Production,
        new Technology("F00000000", "T010000"),
        "meteringPointOwner");

    [Fact]
    public async Task Handle_NoMasterData_NoProducedEvent()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData("gsrn"))
            .ReturnsAsync(null as MasterData);

        var service = new CertificateService(masterDataServiceMock.Object, Mock.Of<ILogger<CertificateService>>());

        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await service.Handle(@event);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Handle_ConsumptionPoint_NoProducedEvent()
    {
        var masterDataForConsumptionPoint = validMasterData with { Type = MeteringPointType.Consumption };

        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock
            .Setup(m => m.GetMasterData(masterDataForConsumptionPoint.GSRN))
            .ReturnsAsync(masterDataForConsumptionPoint);

        var service = new CertificateService(masterDataServiceMock.Object, Mock.Of<ILogger<CertificateService>>());

        var @event = new EnergyMeasured(masterDataForConsumptionPoint.GSRN, new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await service.Handle(@event);

        Assert.Null(producedEvent);
    }

    //[Fact]
    //public async Task Handle_MeteringPointNotOnboarded_NoProducedEvent()
    //{
    //    var masterDataForConsumptionPoint = validMasterData with { Type = MeteringPointType.Consumption };

    //    var masterDataServiceMock = new Mock<IMasterDataService>();
    //    masterDataServiceMock
    //        .Setup(m => m.GetMasterData(masterDataForConsumptionPoint.GSRN))
    //        .ReturnsAsync(masterDataForConsumptionPoint);

    //    var service = new CertificateService(masterDataServiceMock.Object, Mock.Of<ILogger<CertificateService>>());

    //    var @event = new EnergyMeasured(masterDataForConsumptionPoint.GSRN, new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
    //    var producedEvent = await service.Handle(@event);

    //    Assert.Null(producedEvent);
    //}
    
    [Fact]
    public async Task Handle_ProductionPoint_NoProducedEvent()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock
            .Setup(m => m.GetMasterData(validMasterData.GSRN))
            .ReturnsAsync(validMasterData);

        var service = new CertificateService(masterDataServiceMock.Object, Mock.Of<ILogger<CertificateService>>());

        var @event = new EnergyMeasured(validMasterData.GSRN, new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await service.Handle(@event);

        Assert.NotNull(producedEvent);

        var expected = new ProductionCertificateCreated(
            producedEvent!.CertificateId,
            validMasterData.GridArea,
            @event.Period,
            validMasterData.Technology,
            validMasterData.MeteringPointOwner,
            new ShieldedValue<string>(@event.GSRN, 42),
            new ShieldedValue<long>(@event.Quantity, 42));

        Assert.Equal(expected, producedEvent);
    }
}
