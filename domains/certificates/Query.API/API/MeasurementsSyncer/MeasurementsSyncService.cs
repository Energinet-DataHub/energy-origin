using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Measurements.V1;
using Microsoft.Extensions.Logging;
using HashedAttribute = API.ContractService.Clients.HashedAttribute;
using Technology = DataContext.ValueObjects.Technology;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService
{
    public const string AssetId = "AssetId";
    public const string TechCode = "TechCode";
    public const string FuelCode = "FuelCode";

    private readonly ISlidingWindowState slidingWindowState;
    private readonly Measurements.V1.Measurements.MeasurementsClient measurementsClient;
    private readonly SlidingWindowService slidingWindowService;
    private readonly IMeasurementSyncMetrics measurementSyncMetrics;
    private readonly IStampClient stampClient;
    private readonly ILogger<MeasurementsSyncService> logger;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISlidingWindowState slidingWindowState,
        Measurements.V1.Measurements.MeasurementsClient measurementsClient, IPublishEndpoint bus, SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics, IStampClient stampClient)
    {
        this.logger = logger;
        this.slidingWindowState = slidingWindowState;
        this.measurementsClient = measurementsClient;
        this.slidingWindowService = slidingWindowService;
        this.measurementSyncMetrics = measurementSyncMetrics;
        this.stampClient = stampClient;
    }

    public async Task FetchAndPublishMeasurements(MeteringPointSyncInfo syncInfo,
        MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour();
        var fetchedMeasurements = await FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, synchronizationPoint, stoppingToken);

        measurementSyncMetrics.MeasurementsFetched(fetchedMeasurements.Count);

        if (fetchedMeasurements.Count != 0)
        {
            var measurementsToPublish = slidingWindowService.FilterMeasurements(slidingWindow, fetchedMeasurements);

            if (measurementsToPublish.Any())
            {
                await IssueCertificates(measurementsToPublish,
                    syncInfo.MeteringPointType,
                    syncInfo.GridArea,
                    syncInfo.RecipientId,
                    syncInfo.Technology,
                    stoppingToken);
            }

            slidingWindowService.UpdateSlidingWindow(slidingWindow, fetchedMeasurements, synchronizationPoint);
            await slidingWindowState.UpdateSlidingWindow(slidingWindow, stoppingToken);
            await slidingWindowState.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task IssueCertificates(List<Measurement> measurements,
        MeteringPointType meteringPointType,
        string gridArea,
        Guid recipientId,
        Technology? technology,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending {Count} measurements to Stamp", measurements.Count);

        foreach (var m in measurements)
        {
            if (m.Quality != EnergyQuantityValueQuality.Measured && m.Quality != EnergyQuantityValueQuality.Calculated)
                continue;

            if (m.Quantity <= 0)
            {
                logger.LogError("Quantity lower than 0: {0}", m.Quantity);
                continue;
            }

            if (m.Quantity > uint.MaxValue)
            {
                logger.LogError("Quantity too high for measurement. Quantity: {0}", m.Quantity);
                continue;
            }

            var clearTextAttributes = new Dictionary<string, string>();
            if (meteringPointType == MeteringPointType.Production)
            {
                clearTextAttributes.Add(FuelCode, technology!.FuelCode);
                clearTextAttributes.Add(TechCode, technology.TechCode);
            }
            clearTextAttributes.Add(AssetId, m.Gsrn);

            var certificate = new CertificateDto
            {
                Id = Guid.NewGuid(),
                End = m.DateTo,
                Start = m.DateFrom,
                Quantity = (uint)m.Quantity,
                Type = meteringPointType.MapToCertificateType(),
                GridArea = gridArea,
                ClearTextAttributes = clearTextAttributes,
                HashedAttributes = new List<HashedAttribute>()
            };

            await stampClient.IssueCertificate(recipientId, m.Gsrn, certificate, cancellationToken);
        }

        logger.LogInformation("Sent {Count} measurements to Stamp", measurements.Count);
    }

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

        await FetchAndPublishMeasurements(syncInfo,
            slidingWindow,
            stoppingToken);
    }
}
