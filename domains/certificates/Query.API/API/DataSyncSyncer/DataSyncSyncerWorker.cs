using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.ContractService.Repositories;
using API.DataSyncSyncer.Client.Dto;
using API.KeyIssuer;
using Marten;
using MassTransit;
using MeasurementEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IDocumentStore documentStore;
    private readonly DataSyncService dataSyncService;
    private readonly IServiceScopeFactory scopeFactory;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        IDocumentStore documentStore,
        IBus bus,
        DataSyncService dataSyncService,
        IServiceScopeFactory scopeFactory)
    {
        this.bus = bus;
        this.logger = logger;
        this.documentStore = documentStore;
        this.dataSyncService = dataSyncService;
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var allContracts = await GetAllContracts(stoppingToken);
            foreach (var contract in allContracts)
            {
                if (string.IsNullOrEmpty(contract.PublicKey))
                {
                    using var scope = scopeFactory.CreateScope();
                    var keyIssuer = scope.ServiceProvider.GetService<IKeyIssuer>();
                    var certificateIssuingContractRepository = scope.ServiceProvider.GetService<ICertificateIssuingContractRepository>();

                    if (keyIssuer != null && certificateIssuingContractRepository != null)
                    {
                        var publicKey = await keyIssuer.Create(contract.MeteringPointOwner, stoppingToken);
                        contract.PublicKey = publicKey;
                        await certificateIssuingContractRepository.Update(contract);
                    }
                }

                var measurements = await dataSyncService.FetchMeasurements(contract,
                    stoppingToken);

                if (measurements.Any())
                {
                    await PublishIntegrationEvents(measurements, stoppingToken);
                }
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task<IReadOnlyList<CertificateIssuingContract>> GetAllContracts(CancellationToken cancellationToken)
    {
        try
        {
            await using var querySession = documentStore.QuerySession();

            return await querySession
                .Query<CertificateIssuingContract>()
                .ToListAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed fetching contracts. Exception: {e}", e);
            return new List<CertificateIssuingContract>();
        }
    }

    private async Task PublishIntegrationEvents(List<DataSyncDto> measurements, CancellationToken cancellationToken)
    {
        var integrationsEvents = MapToIntegrationEvents(measurements);
        logger.LogInformation(
            "Publishing {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count
        );
        foreach (var @event in integrationsEvents)
        {
            await bus.Publish(@event, cancellationToken);
        }
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }

    private static List<EnergyMeasuredIntegrationEvent> MapToIntegrationEvents(List<DataSyncDto> measurements)
    {
        return measurements
            .Select(it => new EnergyMeasuredIntegrationEvent(
                    GSRN: it.GSRN,
                    DateFrom: it.DateFrom,
                    DateTo: it.DateTo,
                    Quantity: it.Quantity,
                    Quality: it.Quality
                )
            )
            .ToList();
    }
}
