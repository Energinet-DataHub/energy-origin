using System.Threading.Tasks;
using API.ContractService;
using API.GranularCertificateIssuer.Repositories;
using CertificateEvents.Aggregates;
using CertificateEvents.Primitives;
using MassTransit;
using MeasurementEvents;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class EnergyMeasuredConsumer : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly ILogger<EnergyMeasuredConsumer> logger;
    private readonly IProductionCertificateRepository repository;
    private readonly IContractService contractService;

    public EnergyMeasuredConsumer(ILogger<EnergyMeasuredConsumer> logger, IProductionCertificateRepository repository, IContractService contractService)
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
            logger.LogInformation("No production certificate event stream started for {message}", message);
            return;
        }

        var productionCertificate = new ProductionCertificate(
            contract!.GridArea,
            new Period(message.DateFrom, message.DateTo),
            new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            contract.MeteringPointOwner,
            message.GSRN,
            message.Quantity);

        productionCertificate.Issue();

        await repository.Save(productionCertificate, context.CancellationToken);

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
