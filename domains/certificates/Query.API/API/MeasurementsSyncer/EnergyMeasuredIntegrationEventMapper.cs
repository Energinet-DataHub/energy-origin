using System;
using System.Collections.Generic;
using System.Linq;
using DataContext.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V2;
using Measurements.V1;
using Meteringpoint.V1;
using Technology = EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V2.Technology;

namespace API.MeasurementsSyncer;

public class EnergyMeasuredIntegrationEventMapper
{
    public List<EnergyMeasuredIntegrationEvent> MapToIntegrationEvents(MeteringPoint meteringPoint, MeteringPointSyncInfo syncInfo,
        List<Measurement> measurements)
    {
        return measurements.Select(measurement =>
                MapToIntegrationEvent(meteringPoint, syncInfo.MeteringPointType, syncInfo.GridArea, syncInfo.Technology, syncInfo.RecipientId, measurement))
            .ToList();
    }

    private EnergyMeasuredIntegrationEvent MapToIntegrationEvent(MeteringPoint meteringPoint, MeteringPointType meteringPointType, string gridArea,
        DataContext.ValueObjects.Technology? technology, Guid recipientId, Measurement measurement)
    {
        var address = new Address(meteringPoint.StreetName, meteringPoint.BuildingNumber, meteringPoint.CityName, meteringPoint.Postcode, "Denmark");
        return new EnergyMeasuredIntegrationEvent(
            GSRN: measurement.Gsrn,
            Address: address,
            GridArea: gridArea,
            RecipientId: recipientId,
            DateFrom: measurement.DateFrom,
            DateTo: measurement.DateTo,
            Quantity: measurement.Quantity,
            Capacity: meteringPoint.Capacity,
            Technology: MapTechnology(technology),
            MeterType: MapMeterType(meteringPointType),
            Quality: MapQuality(measurement.Quality));
    }

    private MeterType MapMeterType(MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            MeteringPointType.Consumption => MeterType.Consumption,
            MeteringPointType.Production => MeterType.Production,
            _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, null)
        };
    }

    private Technology MapTechnology(DataContext.ValueObjects.Technology? technology)
    {
        return new Technology(technology?.FuelCode ?? "", technology?.TechCode ?? "");
    }

    private Quality MapQuality(EnergyQuantityValueQuality quantity) =>
        quantity switch
        {
            EnergyQuantityValueQuality.Measured => Quality.Measured,
            EnergyQuantityValueQuality.Estimated => Quality.Estimated,
            EnergyQuantityValueQuality.Calculated => Quality.Calculated,
            EnergyQuantityValueQuality.Revised => Quality.Revised,
            _ => throw new ArgumentOutOfRangeException(nameof(quantity), quantity, null)
        };
}
