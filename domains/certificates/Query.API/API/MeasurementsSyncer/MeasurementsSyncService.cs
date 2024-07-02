using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using MeasurementEvents;
using Measurements.V1;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService
{
    private readonly ISlidingWindowState slidingWindowState;
    private readonly Measurements.V1.Measurements.MeasurementsClient measurementsClient;
    private readonly IPublishEndpoint bus;
    private readonly SlidingWindowService slidingWindowService;
    private readonly IMeasurementSyncMetrics measurementSyncMetrics;
    private readonly ILogger<MeasurementsSyncService> logger;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISlidingWindowState slidingWindowState,
        Measurements.V1.Measurements.MeasurementsClient measurementsClient, IPublishEndpoint bus, SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics)
    {
        this.logger = logger;
        this.slidingWindowState = slidingWindowState;
        this.measurementsClient = measurementsClient;
        this.bus = bus;
        this.slidingWindowService = slidingWindowService;
        this.measurementSyncMetrics = measurementSyncMetrics;
    }

    public async Task FetchAndPublishMeasurements(string meteringPointOwner, MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour();
        var fetchedMeasurements = await FetchMeasurements(slidingWindow, meteringPointOwner, synchronizationPoint, stoppingToken);

        measurementSyncMetrics.MeasurementsFetched(fetchedMeasurements.Count);

        if (fetchedMeasurements.Count != 0)
        {
            var measurementsToPublish = slidingWindowService.FilterMeasurements(slidingWindow, fetchedMeasurements);

            if (measurementsToPublish.Any())
            {
                await PublishIntegrationEvents(measurementsToPublish, stoppingToken);
            }

            slidingWindowService.UpdateSlidingWindow(slidingWindow, fetchedMeasurements, synchronizationPoint);
            await slidingWindowState.UpdateSlidingWindow(slidingWindow, stoppingToken);
            await slidingWindowState.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task PublishIntegrationEvents(List<Measurement> measurements, CancellationToken cancellationToken)
    {
        var integrationsEvents = MapToIntegrationEvents(measurements);
        logger.LogInformation("Publishing {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count);

        foreach (var @event in integrationsEvents)
        {
            await bus.Publish(@event, cancellationToken);
        }

        logger.LogInformation("Published {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count);
    }

    private static List<EnergyMeasuredIntegrationEvent> MapToIntegrationEvents(List<Measurement> measurements)
    {
        return measurements
            .Select(it => new EnergyMeasuredIntegrationEvent(
                    GSRN: it.Gsrn,
                    DateFrom: it.DateFrom,
                    DateTo: it.DateTo,
                    Quantity: it.Quantity,
                    Quality: MapQuality(it.Quality)
                )
            )
            .ToList();
    }


    private static MeasurementQuality MapQuality(EnergyQuantityValueQuality q) =>
        q switch
        {
            EnergyQuantityValueQuality.Measured => MeasurementQuality.Measured,
            EnergyQuantityValueQuality.Estimated => MeasurementQuality.Estimated,
            EnergyQuantityValueQuality.Calculated => MeasurementQuality.Calculated,
            EnergyQuantityValueQuality.Revised => MeasurementQuality.Revised,
            _ => throw new ArgumentOutOfRangeException(nameof(q), q, null)
        };

    public async Task<List<Measurement>> FetchMeasurements(MeteringPointTimeSeriesSlidingWindow slidingWindow, string meteringPointOwner,
        UnixTimestamp synchronizationPoint,
        CancellationToken cancellationToken)
    {
        var dateFrom = slidingWindow.GetFetchIntervalStart().Seconds;
        var synchronizationPointSeconds = synchronizationPoint.Seconds;

        if (dateFrom < synchronizationPointSeconds)
        {
            try
            {
                var request = new GetMeasurementsRequest
                {
                    DateFrom = dateFrom,
                    DateTo = synchronizationPointSeconds,
                    Gsrn = slidingWindow.GSRN,
                    Subject = meteringPointOwner,
                    Actor = Guid.NewGuid().ToString()
                };
                var res = await measurementsClient.GetMeasurementsAsync(request, cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Successfully fetched {numberOfMeasurements} measurements for GSRN {GSRN} in period from {from} to: {to}",
                    res.Measurements.Count,
                    slidingWindow.GSRN,
                    DateTimeOffset.FromUnixTimeSeconds(dateFrom).ToString("o"),
                    DateTimeOffset.FromUnixTimeSeconds(synchronizationPointSeconds).ToString("o"));

                return res.Measurements.ToList();
            }
            catch (Exception e)
            {
                logger.LogError("An error occured: {error}, no measurements were fetched", e.Message);
            }
        }

        return new();
    }

    public async Task HandleSingleSyncInfo(MeteringPointSyncInfo syncInfo, CancellationToken stoppingToken)
    {
        var slidingWindow = await slidingWindowState.GetSlidingWindowStartTime(syncInfo, stoppingToken);

        await FetchAndPublishMeasurements(syncInfo.MeteringPointOwner, slidingWindow, stoppingToken);
    }
}
