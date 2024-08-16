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
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient;
    private readonly ILogger<MeasurementsSyncService> logger;

    public MeasurementsSyncService(ILogger<MeasurementsSyncService> logger, ISlidingWindowState slidingWindowState,
        Measurements.V1.Measurements.MeasurementsClient measurementsClient, IPublishEndpoint bus, SlidingWindowService slidingWindowService,
        IMeasurementSyncMetrics measurementSyncMetrics, IStampClient stampClient, Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointsClient)
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
        var meteringPoints = await meteringPointsClient.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest() { Subject = syncInfo.MeteringPointOwner });
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
            var address = meteringPoint.BuildingNumber + " " + meteringPoint.StreetName + " " + meteringPoint.CityName + " " + meteringPoint.Postcode;
            clearTextAttributes.Add(AttributeKeys.AssetId, m.Gsrn);
            clearTextAttributes.Add(AttributeKeys.EnergyTagGcIssuer, "Energinet");
            clearTextAttributes.Add(AttributeKeys.EnergyTagGcIssueMarketZone, syncInfo.GridArea);
            clearTextAttributes.Add(AttributeKeys.EnergyTagCountry, "Denmark");
            clearTextAttributes.Add(AttributeKeys.EnergyTagGcIssuanceDateStamp, DateTimeOffset.Now.ToString("d"));
            clearTextAttributes.Add(AttributeKeys.EnergyTagProductionStartingIntervalTimestamp, m.DateFrom.ToString());
            clearTextAttributes.Add(AttributeKeys.EnergyTagProductionEndingIntervalTimestamp, m.DateTo.ToString());
            clearTextAttributes.Add(AttributeKeys.EnergyTagGcFaceValue, "Wh");
            clearTextAttributes.Add(AttributeKeys.EnergyTagProductionDeviceUniqueIdentification, m.Gsrn);
            clearTextAttributes.Add(AttributeKeys.EnergyTagProducedEnergySource, syncInfo.Technology!.FuelCode);
            clearTextAttributes.Add(AttributeKeys.EnergyTagProducedEnergyTechnology, syncInfo.Technology.TechCode);
            clearTextAttributes.Add(AttributeKeys.EnergyTagConnectedGridIdentification, syncInfo.GridArea);
            clearTextAttributes.Add(AttributeKeys.EnergyTagProductionDeviceLocation, address);
            clearTextAttributes.Add(AttributeKeys.EnergyTagProductionDeviceCapacity, meteringPoint.Capacity);
            clearTextAttributes.Add(AttributeKeys.EnergyTagProductionDeviceCommercialOperationDate, "N/A");
            clearTextAttributes.Add(AttributeKeys.EnergyTagEnergyCarrier, "Electricity");
            clearTextAttributes.Add(AttributeKeys.EnergyTagGcIssueDeviceType, "Production");

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
    public const string EnergyTagGcIssuer = "EnergyTag_GcIssuer";
    public const string EnergyTagGcIssueMarketZone = "EnergyTag_GcIssueMarketZone";
    public const string EnergyTagCountry = "EnergyTag_Country";
    public const string EnergyTagGcIssuanceDateStamp = "EnergyTag_GcIssuanceDatestamp";
    public const string EnergyTagProductionStartingIntervalTimestamp = "EnergyTag_ProductionStartingIntervalTimestamp";
    public const string EnergyTagProductionEndingIntervalTimestamp = "EnergyTag_ProductionEndingIntervalTimestamp";
    public const string EnergyTagGcFaceValue = "EnergyTag_GcFaceValue";
    public const string EnergyTagProductionDeviceUniqueIdentification = "EnergyTag_ProductionDeviceUniqueIdentification";
    public const string EnergyTagProducedEnergySource = "EnergyTag_ProducedEnergySource";
    public const string EnergyTagProducedEnergyTechnology = "EnergyTag_ProducedEnergyTechnology";
    public const string EnergyTagConnectedGridIdentification = "EnergyTag_ConnectedGridIdentification";
    public const string EnergyTagProductionDeviceLocation = "EnergyTag_ProductionDeviceLocation";
    public const string EnergyTagProductionDeviceCapacity = "EnergyTag_ProductionDeviceCapacity";
    public const string EnergyTagProductionDeviceCommercialOperationDate = "EnergyTag_ProductionDeviceCommercialOperationDate";
    public const string EnergyTagEnergyCarrier = "EnergyTag_EnergyCarrier";
    public const string EnergyTagGcIssueDeviceType = "EnergyTag_GcIssueDeviceType";
}
