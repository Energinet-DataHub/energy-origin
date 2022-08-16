using System;
using System.Collections.Generic;
using API.Models;
using EnergyOriginDateTimeExtension;

namespace Tests;

internal class CalculateEmissionDataSetFactory
{
    public List<TimeSeries> CreateTimeSeries()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            (
                new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
                CreateMeasurementsFirstMP()
            ),
            new TimeSeries
            (
                new MeteringPoint("571313121223234324", "DK1", MeterType.Consumption),
                CreateMeasurementsSecondMP()
            )
        };
    }

    public List<TimeSeries> CreateTimeSeriesHugeValues()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            (
                new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
                CreateMeasurementHugeNumbers()
            )
        };
    }


    public List<TimeSeries> CreateTimeSeriesForMismatchMeasurements()
    {
        return new List<TimeSeries>
        {
            new TimeSeries
            (
                new MeteringPoint("571313121223234323", "DK1", MeterType.Consumption),
                CreateMeasurementsFirstMP()
            ),
            new TimeSeries
            (
                new MeteringPoint("571313121223234324", "DK1", MeterType.Consumption),
                CreateMeasurementsForMismatch()
            )
        };
    }

    public List<Measurement> CreateMeasurementsFirstMP()
    {
        return new List<Measurement>
        {
            new Measurement
            {
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1234,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 242,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 654,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1800,
                Quality = Quality.Measured
                },
        };
    }
    public List<Measurement> CreateMeasurementsSecondMP()
    {
        return new List<Measurement>
        {
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 789,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1212,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 324,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1233,
                Quality = Quality.Measured
                }
        };
    }
    public List<Measurement> CreateMeasurementsForMismatch()
    {
        return new List<Measurement>
        {
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 789,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,0, 0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1212,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 324,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1233,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234324",
                DateFrom = new DateTime(2021, 1, 2, 2,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 3,0,0, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 2140,
                Quality = Quality.Measured
                }

        };
    }

    public List<EmissionRecord> CreateEmissions()
    {
        return new List<EmissionRecord>
        {
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 124,
                hourUTC: new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc)
            ),
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 234,
                hourUTC: new DateTime(2021, 1, 1, 23, 0, 0, DateTimeKind.Utc)
            ),
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 85,
                hourUTC: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            ),
            new EmissionRecord
            (
                gridArea: "DK1",
                nOXPerkWh: 0,
                cO2PerkWh: 120,
                hourUTC: new DateTime(2021, 1, 2, 1, 0, 0, DateTimeKind.Utc)
            ),
        };
    }

    public List<Measurement> CreateMeasurementHugeNumbers()
    {
        return new List<Measurement>
        {
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 2000000000ul,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1500000000ul,
                Quality = Quality.Measured
                },
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1000000000ul,
                Quality = Quality.Measured
                },
            new Measurement(){
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1500000000ul,
                Quality = Quality.Measured
                }
        };
    }
}
