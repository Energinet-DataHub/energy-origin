using System;
using System.Numerics;
using System.Threading.Tasks;
using API.ContractService;
using CertificateEvents;
using CertificateEvents.Primitives;
using Marten;
using MassTransit;
using Microsoft.Extensions.Logging;
using MeasurementEvents;

namespace API.GranularCertificateIssuer;

public class EnergyMeasuredConsumer : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly ILogger<EnergyMeasuredConsumer> logger;
    private readonly IDocumentSession session;
    private readonly IContractService contractService;

    public EnergyMeasuredConsumer(ILogger<EnergyMeasuredConsumer> logger, IDocumentSession session, IContractService contractService)
    {
        this.logger = logger;
        this.session = session;
        this.contractService = contractService;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var message = context.Message;

        var contract = await contractService.GetByGSRN(message.GSRN, context.CancellationToken);

        if (!ShouldEventBeProduced(contract, message))
        {
            logger.LogInformation("No production certificate event stream started for {message}", message);
            return;
        }

        var certificateId = Guid.NewGuid();

        var createdEvent = new ProductionCertificateCreated(
            CertificateId: certificateId,
            GridArea: contract!.GridArea,
            Period: new Period(message.DateFrom, message.DateTo),
            Technology: new Technology(
                FuelCode: "F00000000",
                TechCode: "T070000"),
            MeteringPointOwner: contract.MeteringPointOwner,
            ShieldedGSRN: new ShieldedValue<string>(message.GSRN, BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(message.Quantity, BigInteger.Zero));

        var issuedEvent = new ProductionCertificateIssued(
            CertificateId: createdEvent.CertificateId,
            MeteringPointOwner: createdEvent.MeteringPointOwner,
            GSRN: createdEvent.ShieldedGSRN.Value);

        session.Events.StartStream(certificateId, createdEvent, issuedEvent);
        await session.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Created production certificate event stream for {message}", message);
    }

    private static bool ShouldEventBeProduced(CertificateIssuingContract? contract,
        EnergyMeasuredIntegrationEvent energyMeasuredIntegrationEvent)
    {
        if (contract is null)
            return false;

        if (contract.MeteringPointType != MeteringPointType.Production)
            return false;

        if (energyMeasuredIntegrationEvent.DateFrom < contract.StartDate.ToUnixTimeSeconds())
            return false;

        return true;
    }

}
