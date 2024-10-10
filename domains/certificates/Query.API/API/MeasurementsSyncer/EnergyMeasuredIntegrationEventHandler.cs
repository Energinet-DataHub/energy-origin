using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.MeasurementsSyncer.Metrics;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V1;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOriginClients.Models;
using HashedAttribute = API.ContractService.Clients.HashedAttribute;
using MeterType = EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V1.MeterType;

namespace API.MeasurementsSyncer;

public class EnergyMeasuredIntegrationEventHandler : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly IStampClient _stampClient;
    private readonly IMeasurementSyncMetrics _measurementSyncMetrics;
    private readonly ILogger<EnergyMeasuredIntegrationEventHandler> _logger;

    public EnergyMeasuredIntegrationEventHandler(IStampClient stampClient, IMeasurementSyncMetrics measurementSyncMetrics,
        ILogger<EnergyMeasuredIntegrationEventHandler> logger)
    {
        _stampClient = stampClient;
        _measurementSyncMetrics = measurementSyncMetrics;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        await IssueCertificate(context.Message, context.CancellationToken);
    }

    private async Task IssueCertificate(EnergyMeasuredIntegrationEvent measurementEvent, CancellationToken cancellationToken)
    {
        var from = DateTimeOffset.FromUnixTimeSeconds(measurementEvent.DateFrom).ToString("o");
        var to = DateTimeOffset.FromUnixTimeSeconds(measurementEvent.DateTo).ToString("o");
        _logger.LogInformation("Sending measurement to stamp for GSRN {GSRN} in period from {from} to: {to}", measurementEvent.GSRN, from, to);

        var certificate = CreateCertificate(measurementEvent);

        await _stampClient.IssueCertificate(measurementEvent.RecipientId, measurementEvent.GSRN, certificate, cancellationToken);
        _measurementSyncMetrics.AddNumberOfMeasurementsPublished(1);

        _logger.LogInformation("Sent measurement for GSRN {GSRN} in period from {from} to: {to} to Stamp", measurementEvent.GSRN, from, to);
    }

    private CertificateDto CreateCertificate(EnergyMeasuredIntegrationEvent measurementEvent)
    {
        var clearTextAttributes = new Dictionary<string, string>();
        var hashedAttributes = new List<HashedAttribute>();

        if (measurementEvent.MeterType == MeterType.Production)
        {
            var address = measurementEvent.Address;
            hashedAttributes.Add(new HashedAttribute
            { Key = EnergyTagAttributeKeys.EnergyTagProductionDeviceUniqueIdentification, Value = measurementEvent.GSRN });
            hashedAttributes.Add(new HashedAttribute { Key = EnergyTagAttributeKeys.EnergyTagProductionDeviceLocation, Value = address });

            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagGcIssuer, "Energinet");
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagGcIssueMarketZone, measurementEvent.GridArea);
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagCountry, "Denmark");
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagGcIssuanceDateStamp, DateTimeOffset.Now.ToString("d"));
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagProductionStartingIntervalTimestamp, measurementEvent.DateFrom.ToString());
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagProductionEndingIntervalTimestamp, measurementEvent.DateTo.ToString());
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagGcFaceValue, measurementEvent.Quantity.ToString());
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagConnectedGridIdentification, measurementEvent.GridArea);
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagProductionDeviceCapacity, measurementEvent.Capacity);
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagProductionDeviceCommercialOperationDate, "N/A");
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagEnergyCarrier, "Electricity");
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagGcIssueDeviceType, "Production");
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagProducedEnergySource, measurementEvent.Technology.AibFuelCode);
            clearTextAttributes.Add(EnergyTagAttributeKeys.EnergyTagProducedEnergyTechnology, measurementEvent.Technology.AibTechCode);
        }
        else
        {
            hashedAttributes.Add(new HashedAttribute { Key = EnergyTagAttributeKeys.AssetId, Value = measurementEvent.GSRN });
        }

        var certificate = new CertificateDto
        {
            Id = Guid.NewGuid(),
            End = measurementEvent.DateTo,
            Start = measurementEvent.DateFrom,
            Quantity = (uint)measurementEvent.Quantity,
            Type = MapMeterType(measurementEvent.MeterType),
            GridArea = measurementEvent.GridArea,
            ClearTextAttributes = clearTextAttributes,
            HashedAttributes = hashedAttributes
        };
        return certificate;
    }

    private CertificateType MapMeterType(MeterType meterType)
    {
        return meterType switch
        {
            MeterType.Production => CertificateType.Production,
            MeterType.Consumption => CertificateType.Consumption,
            _ => throw new InvalidOperationException($"Invalid meter type {meterType}")
        };
    }
}

public class EnergyMeasuredIntegrationEventHandlerDefinition : ConsumerDefinition<EnergyMeasuredIntegrationEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<EnergyMeasuredIntegrationEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(10, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)));
    }
}