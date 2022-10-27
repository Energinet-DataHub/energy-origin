using System;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using Issuer.Worker.MasterDataService;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.GranularCertificateIssuer;

public class EnergyMeasuredEventHandler : IEnergyMeasuredEventHandler
{
    private readonly IMasterDataService masterDataService;
    private readonly ILogger<EnergyMeasuredEventHandler> logger;

    public EnergyMeasuredEventHandler(IMasterDataService masterDataService, ILogger<EnergyMeasuredEventHandler> logger)
    {
        this.masterDataService = masterDataService;
        this.logger = logger;
    }

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
            new ShieldedValue<string>(@event.GSRN, 42),
            new ShieldedValue<long>(@event.Quantity, 42));
    }
}

public interface IEnergyMeasuredEventHandler
{
    Task<ProductionCertificateCreated?> Handle(EnergyMeasured @event);
}
