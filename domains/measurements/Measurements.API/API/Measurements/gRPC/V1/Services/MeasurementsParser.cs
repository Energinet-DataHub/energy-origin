using System;
using System.Collections.Generic;
using System.Linq;
using API.Measurements.Helpers;
using Measurements.V1;
using Metertimeseries.V1;

namespace API.Measurements.gRPC.V1.Services;

public class MeasurementsParser
{
    public List<Measurement> ParseMeasurements(GetMeasurementsRequest request, MeterTimeSeriesResponse dhResponse)
    {
        var meteringPoints = dhResponse.GetMeterTimeSeriesResult.MeterTimeSeriesMeteringPoint;
        var expectedMeasurementCount = (int)(request.DateTo - request.DateFrom) / 3600;
        var result = new List<Measurement>(Math.Max(expectedMeasurementCount, 0));

        foreach (var meteringPoint in meteringPoints)
        {
            var meteringPointStates = meteringPoint.MeteringPointStates;
            foreach (var state in meteringPointStates)
            {
                // TODO: Assumes a single metering point state for each metering point, this may break if ex. meter reading occurrence changes in requested period
                foreach (var quantity in state.NonProfiledEnergyQuantities)
                {
                    if (quantity.EnergyQuantityValues.Count == 0)
                    {
                        continue;
                    }

                    var meterReadingOccurrence = GetActualMeterReadingOccurrence(quantity, state.MeterReadingOccurrence);
                    result.AddRange(ParseMeasurements(meteringPoint, quantity, meterReadingOccurrence, request));
                }
            }
        }

        return result;
    }

    private List<Measurement> ParseMeasurements(MeterTimeSeriesMeteringPoint meteringPoint, NonProfiledEnergyQuantity nonProfiledEnergyQuantity,
        string meterReadingOccurrence, GetMeasurementsRequest request)
    {
        var measurements = nonProfiledEnergyQuantity.EnergyQuantityValues
            .Select(value => ParseMeasurement(meteringPoint, nonProfiledEnergyQuantity, value, meterReadingOccurrence))
            .Where(measurement => measurement.Gsrn == request.Gsrn)
            .Where(measurement => measurement.DateFrom >= request.DateFrom && measurement.DateTo <= request.DateTo)
            .ToList();

        if (meterReadingOccurrence == MeterTimeSeriesHelper.HourlyMeasurementsOccurrence)
        {
            return measurements;
        }

        return measurements.GroupBy(measurement => measurement.DateFrom.ZeroedHour())
            .Select(group => new Measurement
            {
                Gsrn = meteringPoint.MeteringPointId,
                DateFrom = group.Min(m => m.DateFrom),
                DateTo = group.Max(m => m.DateTo),
                Quantity = group.Sum(m => m.Quantity),
                Quality = group.Max(m => m.Quality),
                QuantityMissing = group.Any(m => m.QuantityMissing)
            }).ToList();
    }

    private Measurement ParseMeasurement(MeterTimeSeriesMeteringPoint meteringPoint, NonProfiledEnergyQuantity quantity, EnergyQuantityValue value,
        string resolution)
    {
        var measurement = new Measurement
        {
            Gsrn = meteringPoint.MeteringPointId,
            DateFrom = MeterTimeSeriesHelper.GetDateTimeFromMeterReadingOccurrence(quantity.Date, int.Parse(value.Position) - 1, resolution),
            DateTo = MeterTimeSeriesHelper.GetDateTimeFromMeterReadingOccurrence(quantity.Date, int.Parse(value.Position), resolution),
            Quantity = MeterTimeSeriesHelper.GetQuantityFromMeterReading(value.EnergyTimeSeriesMeasureUnit, value.EnergyQuantity),
            Quality = MeterTimeSeriesHelper.GetQuantityQualityFromMeterReading(value.QuantityQuality),
            QuantityMissing = MeterTimeSeriesHelper.GetQuantityMissingFromMeterReading(value.QuantityMissingIndicator)
        };
        return measurement;
    }

    private string GetActualMeterReadingOccurrence(NonProfiledEnergyQuantity quantity, string meterReadingOccurrence)
    {
        if (quantity.EnergyQuantityValues.Count is 24 or 23 or 25)
        {
            meterReadingOccurrence = MeterTimeSeriesHelper.HourlyMeasurementsOccurrence;
        }
        else if (quantity.EnergyQuantityValues.Count is 96 or 92 or 100)
        {
            meterReadingOccurrence = MeterTimeSeriesHelper.QuarterlyMeasurementsOccurrence;
        }
        else if (quantity.EnergyQuantityValues.Count < 23)
        {
            // TODO: Assume hour resolution, is this correct? This will support data-hub-face-mock because it may return less than 24 measurements
            meterReadingOccurrence = MeterTimeSeriesHelper.HourlyMeasurementsOccurrence;
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid number of measurements {quantity.EnergyQuantityValues.Count}, meterReadingOccurrence value {meterReadingOccurrence}");
        }

        return meterReadingOccurrence;
    }
}
