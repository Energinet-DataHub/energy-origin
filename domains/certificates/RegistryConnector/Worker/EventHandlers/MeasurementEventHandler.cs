using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.PedersenCommitment;
using System;
using System.Linq;
using System.Threading.Tasks;
using RegistryConnector.Worker.Exceptions;

namespace RegistryConnector.Worker.EventHandlers;

public class MeasurementEventHandler : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly ILogger<MeasurementEventHandler> logger;

    public MeasurementEventHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<MeasurementEventHandler> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var message = context.Message;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var contracts = await dbContext.Contracts.AsNoTracking().Where(c => c.GSRN == message.GSRN)
            .ToListAsync(context.CancellationToken);
        var matchingContract = contracts.Find(c => ShouldEventBeProduced(c, message));
        if (matchingContract == null)
        {
            logger.LogInformation("No certificate created for {Message}", message);
            return;
        }

        if (message.Quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {message.Quantity} to uint");

        var quantity = (uint)message.Quantity;
        var commitment = new SecretCommitmentInfo(quantity);
        var gsrn = new Gsrn(matchingContract.GSRN);

        var period = new Period(message.DateFrom, message.DateTo);
        var walletDepositEndpointPosition = period.CalculateWalletDepositEndpointPosition();
        if (!walletDepositEndpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet position for period {period}");

        Guid certificateId;
        Technology? technology;
        if (matchingContract.MeteringPointType == MeteringPointType.Production)
        {
            technology = matchingContract.Technology!;

            var productionCertificate = new ProductionCertificate(
                matchingContract.GridArea,
                period,
                technology,
                matchingContract.MeteringPointOwner,
                message.GSRN,
                message.Quantity,
                commitment.BlindingValue.ToArray());

            logger.LogInformation("Creating production certificate for {Message} with {certificateId}", message, productionCertificate.Id);

            dbContext.Add(productionCertificate);

            certificateId = productionCertificate.Id;
            logger.LogInformation("Created production certificate for {certificateId}", productionCertificate.Id);
        }
        else if (matchingContract.MeteringPointType == MeteringPointType.Consumption)
        {
            technology = null;
            var consumptionCertificate = new ConsumptionCertificate(
                matchingContract.GridArea,
                period,
                matchingContract.MeteringPointOwner,
                message.GSRN,
                message.Quantity,
                commitment.BlindingValue.ToArray());

            logger.LogInformation("Creating consumption certificate for {Message} with {certificateId}", message, consumptionCertificate.Id);

            dbContext.Add(consumptionCertificate);

            certificateId = consumptionCertificate.Id;
            logger.LogInformation("Created consumption certificate for {certificateId}", message);
        }
        else
            throw new CertificateDomainException(string.Format("Unsupported metering point type {0} for message {1}",
                matchingContract.MeteringPointType, message));

        await context.Publish<CertificateCreatedEvent>(new CertificateCreatedEvent
        {
            MeteringPointType = matchingContract.MeteringPointType,
            Period = period,
            Quantity = quantity,
            CertificateId = certificateId,
            WalletPublicKey = matchingContract.WalletPublicKey,
            WalletUrl = matchingContract.WalletUrl,
            GridArea = matchingContract.GridArea,
            Gsrn = gsrn,
            WalletDepositEndpointPosition = walletDepositEndpointPosition.Value,
            Technology = technology
        });
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private bool ShouldEventBeProduced(CertificateIssuingContract contract,
        EnergyMeasuredIntegrationEvent energyMeasuredIntegrationEvent)
    {
        logger.LogInformation("Checking if measurement {Measurement} should be produced for contract {Contract}",
            energyMeasuredIntegrationEvent, contract);

        if (!contract.Contains(energyMeasuredIntegrationEvent.DateFrom, energyMeasuredIntegrationEvent.DateTo))
            return false;

        if (energyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (energyMeasuredIntegrationEvent.Quantity > uint.MaxValue)
            return false;

        if (energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured &&
            energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Calculated)
            return false;

        return true;
    }
}

public class MeasurementEventHandlerDefinition : ConsumerDefinition<MeasurementEventHandler>
{
    private readonly RetryOptions retryOptions;

    public MeasurementEventHandlerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<MeasurementEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3))
            .Handle(typeof(DbUpdateException), typeof(InvalidOperationException)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}
