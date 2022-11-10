using System;
using System.Numerics;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using Marten;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class EnergyMeasConsumer : IConsumer<Measurement>
{
    private readonly ILogger<EnergyMeasConsumer> logger;
    private readonly IDocumentSession session;
    private readonly IMasterDataService masterDataService;

    public EnergyMeasConsumer(ILogger<EnergyMeasConsumer> logger, IDocumentSession session, IMasterDataService masterDataService)
    {
        this.logger = logger;
        this.session = session;
        this.masterDataService = masterDataService;
    }

    public async Task Consume(ConsumeContext<Measurement> context)
    {
        var message = context.Message;
        logger.LogInformation("Got {meas}", message);
        var masterData = await masterDataService.GetMasterData(message.GSRN);
        if (!ShouldEventBeProduced(masterData))
        {
            return;
        }

        var certificateId = Guid.NewGuid();

        var event1 = new ProductionCertificateCreated(
            CertificateId: certificateId,
            GridArea: masterData!.GridArea,
            Period: message.Period,
            Technology: masterData.Technology,
            MeteringPointOwner: masterData.MeteringPointOwner,
            ShieldedGSRN: new ShieldedValue<string>(message.GSRN, BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(message.Quantity, BigInteger.Zero));

        var event2 = new ProductionCertificateIssued(event1.CertificateId, event1.MeteringPointOwner, event1.ShieldedGSRN.Value);

        session.Events.StartStream(certificateId, event1, event2);
        await session.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Saved events");
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
