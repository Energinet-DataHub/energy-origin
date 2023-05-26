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
        var message = context.Message;

        var contract = await contractService.GetByGSRN(message.GSRN, context.CancellationToken);

        if (!ShouldEventBeProduced(contract, message))
        {
            logger.LogInformation("No production certificate created for {message}", message);
            return;
        }

        var productionCertificate = new ProductionCertificate(
            contract!.GridArea,
            new Period(message.DateFrom, message.DateTo),
            new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            contract.MeteringPointOwner,
            message.GSRN,
            message.Quantity);

        await repository.Save(productionCertificate, context.CancellationToken);

        //TODO handle R values. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1517
        //TODO Save to eventstore and publish event must happen in same transaction. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1518 
        await context.Publish(new ProductionCertificateCreatedEvent(productionCertificate.Id,
            contract.GridArea,
            new Period(message.DateFrom, message.DateTo),
            new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            contract.MeteringPointOwner,
            new ShieldedValue<Gsrn>(new Gsrn(message.GSRN), BigInteger.Zero),
            new ShieldedValue<long>(message.Quantity, BigInteger.Zero)));

        logger.LogInformation("Created production certificate for {message}", message);
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

        if (energyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured)
            return false;

        return true;
    }
}
