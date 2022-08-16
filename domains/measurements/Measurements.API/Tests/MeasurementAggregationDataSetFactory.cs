using API.Models;
using EnergyOriginDateTimeExtension;
using System;
using System.Collections.Generic;

namespace Tests;
internal class MeasurementAggregationDataSetFactory
{
    public List<Measurement> CreateMeasurements()
    {
        return new List<Measurement>
        {
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1234ul,
                Quality = Quality.Measured},
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 242ul,
                Quality = Quality.Measured},
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 654ul,
                Quality = Quality.Measured},
            new Measurement(){
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1800ul,
                Quality = Quality.Measured}
        };
    }

    public List<Measurement> CreateMeasurementHuge()
    {
        return new List<Measurement>
        {
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 22,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 2000000000ul,
                Quality = Quality.Measured},
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 1, 23,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 1, 23,59, 59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1500000000ul,
                Quality = Quality.Measured},
            new Measurement{
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 0,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 0,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1000000000ul,
                Quality = Quality.Measured},
            new Measurement(){
                GSRN = "571313121223234323",
                DateFrom = new DateTime(2021, 1, 2, 1,0,0, DateTimeKind.Utc).ToUnixTime(),
                DateTo = new DateTime(2021, 1, 2, 1,59,59, DateTimeKind.Utc).ToUnixTime(),
                Quantity = 1500000000ul,
                Quality = Quality.Measured}
        };
    }
}
