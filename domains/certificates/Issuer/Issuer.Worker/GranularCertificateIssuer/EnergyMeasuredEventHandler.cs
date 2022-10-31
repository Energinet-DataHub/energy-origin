using System;
using System.Numerics;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using Issuer.Worker.MasterDataService;

namespace Issuer.Worker.GranularCertificateIssuer;

public class EnergyMeasuredEventHandler : IEnergyMeasuredEventHandler
{
    private readonly IMasterDataService masterDataService;

    public EnergyMeasuredEventHandler(IMasterDataService masterDataService) => this.masterDataService = masterDataService;

    public async Task<ProductionCertificateCreated?> Handle(EnergyMeasured @event)
    {
        var masterData = await masterDataService.GetMasterData(@event.GSRN);
        if (masterData is null)
            return null;

        if (masterData.Type != MeteringPointType.Production)
            return null;

        if (!masterData.MeteringPointOnboarded)
            return null;

        return new ProductionCertificateCreated(
            Guid.NewGuid(),
            masterData.GridArea,
            @event.Period,
            masterData.Technology,
            masterData.MeteringPointOwner,
            new ShieldedValue<string>(@event.GSRN, BigInteger.Zero),
            new ShieldedValue<long>(@event.Quantity, BigInteger.Zero));
    }
}
