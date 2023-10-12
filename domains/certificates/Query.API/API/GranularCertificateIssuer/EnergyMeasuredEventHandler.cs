using System.Linq;
using System.Threading.Tasks;
using API.ContractService;
using API.Data;
using CertificateValueObjects;
using Contracts.Certificates;
using MassTransit;
using MeasurementEvents;
using Microsoft.Extensions.Logging;
using ProjectOrigin.PedersenCommitment;

namespace API.GranularCertificateIssuer;

public class EnergyMeasuredEventHandler : IConsumer<ProductionEnergyMeasuredIntegrationEvent>
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

    public async Task Consume(ConsumeContext<ProductionEnergyMeasuredIntegrationEvent> context)
    {
        var message = context.Message;

        var contracts = await contractService.GetByGSRN(message.GSRN, context.CancellationToken);
        var matchingContract = contracts.FirstOrDefault(c => ShouldEventBeProduced(c, message));
        if (matchingContract == null)
        {
            logger.LogInformation("No production certificate created for {Message}", message);
            return;
        }

        var commitment = new SecretCommitmentInfo((uint)message.Quantity);

        var period = new Period(message.DateFrom, message.DateTo);
        var walletDepositEndpointPosition = period.CalculateWalletDepositEndpointPosition();
        if (!walletDepositEndpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet position for period {period}");

        var productionCertificate = new ProductionCertificate(
            matchingContract.GridArea,
            period,
            new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            matchingContract.MeteringPointOwner,
            message.GSRN,
            message.Quantity,
            commitment.BlindingValue.ToArray());

        await repository.Save(productionCertificate, context.CancellationToken);

        //TODO Save to eventstore and publish event must happen in same transaction. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1518
        await context.Publish(new ProductionCertificateCreatedEvent(
            productionCertificate.Id,
            matchingContract.GridArea,
            period,
            new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            matchingContract.MeteringPointOwner,
            new Gsrn(message.GSRN),
            commitment.BlindingValue.ToArray(),
            message.Quantity,
            matchingContract.WalletPublicKey,
            matchingContract.WalletUrl,
            walletDepositEndpointPosition.Value));

        logger.LogInformation("Created production certificate for {Message}", message);
    }

    private static bool ShouldEventBeProduced(CertificateIssuingContract? contract, ProductionEnergyMeasuredIntegrationEvent productionEnergyMeasuredIntegrationEvent)
    {
        if (contract is null)
            return false;

        if (contract.MeteringPointType != MeteringPointType.Production)
            return false;

        if (!contract.Contains(productionEnergyMeasuredIntegrationEvent.DateFrom, productionEnergyMeasuredIntegrationEvent.DateTo))
            return false;

        if (productionEnergyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (productionEnergyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured)
            return false;

        return true;
    }
}
