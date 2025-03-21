using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using DataContext.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V3;
using Meteringpoint.V1;
using Technology = EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V3.Technology;

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
        var address = new Address(meteringPoint.StreetName, meteringPoint.BuildingNumber, meteringPoint.FloorId, meteringPoint.RoomId, meteringPoint.Postcode, meteringPoint.CityName, "Danmark");
        return new EnergyMeasuredIntegrationEvent(
            GSRN: measurement.Gsrn,
            Address: address,
            GridArea: gridArea,
            RecipientId: recipientId,
            DateFrom: measurement.DateFrom,
            DateTo: measurement.DateTo,
            Quantity: measurement.Quantity.ToWattHours(),
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

    private Quality MapQuality(EnergyQuality quantity) =>
        quantity switch
        {
            EnergyQuality.Measured => Quality.Measured,
            EnergyQuality.Estimated => Quality.Estimated,
            EnergyQuality.Calculated => Quality.Calculated,
            _ => throw new ArgumentOutOfRangeException(nameof(quantity), quantity, null)
        };
}
