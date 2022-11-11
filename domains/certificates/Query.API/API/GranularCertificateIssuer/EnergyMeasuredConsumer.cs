using System;
using System.Numerics;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using IntegrationEvents;
using Marten;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class EnergyMeasuredConsumer : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly ILogger<EnergyMeasuredConsumer> logger;
    private readonly IDocumentSession session;
    private readonly IMasterDataService masterDataService;

    public EnergyMeasuredConsumer(ILogger<EnergyMeasuredConsumer> logger, IDocumentSession session, IMasterDataService masterDataService)
    {
        this.logger = logger;
        this.session = session;
        this.masterDataService = masterDataService;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var message = context.Message;

        var masterData = await masterDataService.GetMasterData(message.GSRN);

        if (!ShouldEventBeProduced(masterData))
        {
            logger.LogInformation("No production certificate event stream started for {message}", message);
            return;
        }

        // TODO: Is this the best choice for an ID?
        var certificateId = Guid.NewGuid();

        var createdEvent = new ProductionCertificateCreated(
            CertificateId: certificateId,
            GridArea: masterData!.GridArea,
            Period: new Period(message.DateFrom, message.DateTo),
            Technology: masterData.Technology,
            MeteringPointOwner: masterData.MeteringPointOwner,
            ShieldedGSRN: new ShieldedValue<string>(message.GSRN, BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(message.Quantity, BigInteger.Zero));

        var issuedEvent = new ProductionCertificateIssued(createdEvent.CertificateId, createdEvent.MeteringPointOwner, createdEvent.ShieldedGSRN.Value);

        session.Events.StartStream(certificateId, createdEvent, issuedEvent);
        await session.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Created production certificate event stream for {message}", message);
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
