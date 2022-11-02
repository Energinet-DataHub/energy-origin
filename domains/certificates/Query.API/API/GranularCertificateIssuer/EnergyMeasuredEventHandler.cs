using System;
using System.Numerics;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;

namespace API.GranularCertificateIssuer;

public class EnergyMeasuredEventHandler : IEnergyMeasuredEventHandler
{
    private readonly IMasterDataService masterDataService;

    public EnergyMeasuredEventHandler(IMasterDataService masterDataService) => this.masterDataService = masterDataService;

    public async Task<ProductionCertificateCreated?> Handle(EnergyMeasured @event)
    {
        var masterData = await masterDataService.GetMasterData(@event.GSRN);

        return ShouldEventBeProduced(masterData)
            ? new ProductionCertificateCreated(
                CertificateId: Guid.NewGuid(),
                GridArea: masterData.GridArea,
                Period: @event.Period,
                Technology: masterData.Technology,
                MeteringPointOwner: masterData.MeteringPointOwner,
                ShieldedGSRN: new ShieldedValue<string>(@event.GSRN, BigInteger.Zero),
                ShieldedQuantity: new ShieldedValue<long>(@event.Quantity, BigInteger.Zero))
            : null;
    }

    private static bool ShouldEventBeProduced(MasterData? masterData)
    {
        if (masterData is null)
            return false;

        if (masterData.Type != MeteringPointType.Production)
            return false;

        if (!masterData.MeteringPointOnboarded)
            return false;

        return true;
    }
}
