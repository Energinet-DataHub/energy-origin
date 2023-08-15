using System.Numerics;
using System.Threading.Tasks;
using AggregateRepositories;
using API.ContractService;
using CertificateEvents.Aggregates;
using CertificateValueObjects;
using Contracts.Certificates;
using MassTransit;
using MeasurementEvents;
using Microsoft.Extensions.Logging;
using ProjectOrigin.PedersenCommitment;

namespace API.GranularCertificateIssuer;

public class EnergyMeasuredEventHandler : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly ILogger<EnergyMeasuredEventHandler> logger;
    private readonly IProductionCertificateRepository repository;
    private readonly IContractService contractService;

    public EnergyMeasuredEventHandler(ILogger<EnergyMeasuredEventHandler> logger, IProductionCertificateRepository repository, IContractService contractService)
    {
        this.logger = logger;
        this.repository = repository;
        this.contractService = contractService;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var shouldProduceNoCertificateLogStatement = true;

        var message = context.Message;

        var contracts = await contractService.GetByGSRN(message.GSRN, context.CancellationToken);

        foreach (var contract in contracts)
        {
            if (!ShouldEventBeProduced(contract, message))
            {
                continue;
            }

            var commitment = new SecretCommitmentInfo((uint)message.Quantity);

            var productionCertificate = new ProductionCertificate(
                contract.GridArea,
                new Period(message.DateFrom, message.DateTo),
                new Technology(FuelCode: "F00000000", TechCode: "T070000"),
                contract.MeteringPointOwner,
                message.GSRN,
                message.Quantity);  //TODO: Save commitment

            await repository.Save(productionCertificate, context.CancellationToken);

            //TODO handle R values. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1517. Check if this can be closed...
            //TODO Save to eventstore and publish event must happen in same transaction. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1518
            await context.Publish(new ProductionCertificateCreatedEvent(productionCertificate.Id,
                contract.GridArea,
                new Period(message.DateFrom, message.DateTo),
                new Technology(FuelCode: "F00000000", TechCode: "T070000"),
                contract.MeteringPointOwner,
                new Gsrn(message.GSRN),
                commitment.BlindingValue.ToArray(),
                message.Quantity));

            logger.LogInformation("Created production certificate for {Message}", message);

            shouldProduceNoCertificateLogStatement = false;
        }

        if (shouldProduceNoCertificateLogStatement)
        {
            logger.LogInformation("No production certificate created for {Message}", message);
        }
    }

    private static bool ShouldEventBeProduced(CertificateIssuingContract? contract,
        EnergyMeasuredIntegrationEvent energyMeasuredIntegrationEvent)
    {
        if (contract is null)
            return false;

        if (contract.MeteringPointType != MeteringPointType.Production)
            return false;

        if (!contract.Contains(energyMeasuredIntegrationEvent.DateFrom, energyMeasuredIntegrationEvent.DateTo))
            return false;

        if (energyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured)
            return false;

        return true;
    }
}
