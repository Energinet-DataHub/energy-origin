using System;
using System.Threading.Tasks;
using AggregateRepositories;
using API.ContractService;
using CertificateEvents.Aggregates;
using CertificateEvents.Primitives;
using Marten;
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
        var shouldProduceNoCertificateLogStatement = true;

        var message = context.Message;

        var contracts = await contractService.GetByGSRN(message.GSRN, context.CancellationToken);

        if (contracts.IsEmpty())
        {
            return;
        }

        foreach (var contract in contracts)
        {
            if (!ShouldEventBeProduced(contract, message))
            {
                continue;
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

        if (energyMeasuredIntegrationEvent.DateFrom < contract.StartDate.ToUnixTimeSeconds())
            return false;

        if (energyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured)
            return false;

        if (!CheckEndDateNotNullOrAfterEvent(contract.EndDate, energyMeasuredIntegrationEvent.DateTo))
        {
            return false;
        }

        return true;
    }

    private static bool CheckEndDateNotNullOrAfterEvent(DateTimeOffset? contractEndDate, long eventEndDate)
    {
        return contractEndDate != null && eventEndDate > contractEndDate?.ToUnixTimeSeconds();
    }
}
