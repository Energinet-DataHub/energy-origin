using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Measurements.V1;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;
using HashedAttribute = API.ContractService.Clients.HashedAttribute;
using MeteringPoint = Meteringpoint.V1.MeteringPoint;
using Technology = DataContext.ValueObjects.Technology;

namespace API.MeasurementsSyncer;

public class MeasurementsSyncService
{
    private readonly ISlidingWindowState slidingWindowState;
    private readonly Measurements.V1.Measurements.MeasurementsClient measurementsClient;
    private readonly SlidingWindowService slidingWindowService;
    private readonly IMeasurementSyncMetrics measurementSyncMetrics;
    private readonly IStampClient stampClient;
    private readonly IMeteringPointsClient meteringPointsClient;
    private readonly HttpClient httpClient;
    private readonly ILogger<MeasurementsSyncService> logger;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISlidingWindowState slidingWindowState,
        Measurements.V1.Measurements.MeasurementsClient measurementsClient, IPublishEndpoint bus, SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics, IStampClient stampClient, IMeteringPointsClient meteringPointsClient)
    {
        this.logger = logger;
        this.slidingWindowState = slidingWindowState;
        this.measurementsClient = measurementsClient;
        this.slidingWindowService = slidingWindowService;
        this.measurementSyncMetrics = measurementSyncMetrics;
        this.stampClient = stampClient;
        this.meteringPointsClient = meteringPointsClient;
    }

    public async Task FetchAndPublishMeasurements(MeteringPointSyncInfo syncInfo,
        MeteringPointTimeSeriesSlidingWindow slidingWindow,
        CancellationToken stoppingToken)
    {
        var synchronizationPoint = UnixTimestamp.Now().RoundToLatestHour();
        var fetchedMeasurements = await FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, synchronizationPoint, stoppingToken);
        var meteringPoints = await meteringPointsClient.GetMeteringPoints(new OwnedMeteringPointsRequest() { Subject = syncInfo.MeteringPointOwner });
        var meteringPoint = meteringPoints.MeteringPoints.First(mp => mp.MeteringPointId == slidingWindow.GSRN);

        measurementSyncMetrics.MeasurementsFetched(fetchedMeasurements.Count);

        if (fetchedMeasurements.Count != 0)
        {
            var measurementsToPublish = slidingWindowService.FilterMeasurements(slidingWindow, fetchedMeasurements);

            if (measurementsToPublish.Any())
            {
                await IssueCertificates(measurementsToPublish, meteringPoint,
                    syncInfo,
                    stoppingToken);
            }

            slidingWindowService.UpdateSlidingWindow(slidingWindow, fetchedMeasurements, synchronizationPoint);
            await slidingWindowState.UpdateSlidingWindow(slidingWindow, stoppingToken);
            await slidingWindowState.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task IssueCertificates(List<Measurement> measurements, MeteringPoint meteringPoint,
        MeteringPointSyncInfo syncInfo,
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
            if (syncInfo.MeteringPointType == MeteringPointType.Production)
            {
                clearTextAttributes.Add(AttributeKeys.FuelCode, syncInfo.Technology!.FuelCode);
                clearTextAttributes.Add(AttributeKeys.TechCode, syncInfo.Technology.TechCode);
            }
            clearTextAttributes.Add(AttributeKeys.AssetId, m.Gsrn);
            clearTextAttributes.Add(AttributeKeys.MeteringPointCapacity, meteringPoint.Capacity);
            clearTextAttributes.Add(AttributeKeys.MeteringPointAlias, meteringPoint.MeteringPointAlias);
            clearTextAttributes.Add(AttributeKeys.ConsumerStartDate, "start date");
            clearTextAttributes.Add(AttributeKeys.ZipCode, meteringPoint.Postcode);
            clearTextAttributes.Add(AttributeKeys.StreetName, meteringPoint.StreetName);
            clearTextAttributes.Add(AttributeKeys.CityName, meteringPoint.CityName);
            clearTextAttributes.Add(AttributeKeys.BuildingNumber, meteringPoint.BuildingNumber);
            clearTextAttributes.Add(AttributeKeys.GCIssuer, "Energinet");
            clearTextAttributes.Add(AttributeKeys.Purpose, "Can be used for documentation of energy origin");
            clearTextAttributes.Add(AttributeKeys.Conversion, "Has not been converted");
            clearTextAttributes.Add(AttributeKeys.Configuration, "Config-1");

            var certificate = new CertificateDto
            {
                Id = Guid.NewGuid(),
                End = m.DateTo,
                Start = m.DateFrom,
                Quantity = (uint)m.Quantity,
                Type = syncInfo.MeteringPointType.MapToCertificateType(),
                GridArea = syncInfo.GridArea,
                ClearTextAttributes = clearTextAttributes,
                HashedAttributes = new List<HashedAttribute>()
            };

            await stampClient.IssueCertificate(syncInfo.RecipientId, m.Gsrn, certificate, cancellationToken);
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

public static class AttributeKeys
{
    public const string AssetId = "AssetId";
    public const string TechCode = "TechCode";
    public const string FuelCode = "FuelCode";
    public const string MeteringPointCapacity = "MeteringPointCapacity";
    public const string MeteringPointAlias = "MeteringPointAlias";
    public const string ConsumerStartDate = "ConsumerStartDate";
    public const string ZipCode = "ZipCode";
    public const string StreetName = "StreetName";
    public const string CityName = "CityName";
    public const string BuildingNumber = "BuildingNumber";
    public const string GCIssuer = "GC Issuer";
    public const string Purpose = "Purpose";
    public const string Conversion = "Conversion";
    public const string Configuration = "Configuration";
}
