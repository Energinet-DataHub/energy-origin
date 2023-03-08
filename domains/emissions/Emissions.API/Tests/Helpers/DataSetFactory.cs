
using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Models.EnergiDataService;

namespace Tests.Helpers;

public class DataSetFactory
{
    public static List<TimeSeries> CreateTimeSeries(List<string>? gsrns = default, DateTimeOffset? startingAt = default, int amount = 4)
    {
        gsrns ??= new List<string>() {
            "571313121223234323",
            "571313121223234324"
        };
        startingAt ??= new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);

        return gsrns.Select(x => new TimeSeries(
            new MeteringPoint(x, "DK1", MeterType.Consumption),
            CreateMeasurements(x, startingAt, amount))
        ).ToList();
    }

    public static List<MixRecord> CreateMixSeries(DateTimeOffset? startingAt = default, int amount = 24)
    {
        var start = startingAt ?? new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);
        return Enumerable.Range(0, amount).SelectMany(cursor => CreateMix(start.AddHours(cursor).DateTime)).ToList();
    }

    public static List<EmissionRecord> CreateEmissionSeries(DateTimeOffset? startingAt = default, int amount = 24)
    {
        var start = startingAt ?? new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);
        return Enumerable.Range(0, amount).Select(cursor => CreateEmission(start.AddHours(cursor))).ToList();
    }

    private static EmissionRecord CreateEmission(DateTimeOffset date)
    {
        var hours = (int)Math.Abs((new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).DateTime - date).TotalHours);
        var co2 = hours % 12 * 100;
        return new EmissionRecord(
            gridArea: "DK1",
            nOXPerkWh: 0,
            cO2PerkWh: co2,
            hourUTC: date.UtcDateTime);
    }

    private static List<MixRecord> CreateMix(DateTime date)
    {
        var types = new List<string>() { "WindOnshore", "Solar", "BioGas" };
        var shares = new List<List<int>>
        {
            new() { 20, 40, 40 },
            new() { 40, 20, 40 },
            new() { 40, 40, 20 },
            new() { 50, 30, 20 },
            new() { 30, 50, 20 },
            new() { 20, 30, 50 },
            new() { 90, 10, 0 },
            new() { 10, 90, 0 },
            new() { 100, 0, 0 },
        };
        var hours = (int)Math.Abs((new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).DateTime - date).TotalHours);
        var share = shares[hours % shares.Count]!;
        return Enumerable.Zip(types, share).Select(x => new MixRecord(x.Second, date, "Final", "DK1", x.First)).ToList();
    }

    private static List<Measurement> CreateMeasurements(string gsrn = "571313121223234323", DateTimeOffset? startingAt = default, int amount = 24)
    {
        var start = startingAt ?? DateTimeOffset.Now;
        return Enumerable.Range(0, amount).Select(cursor => new Measurement(
                GSRN: gsrn,
                DateFrom: start.AddHours(cursor).ToUnixTimeSeconds(),
                DateTo: start.AddHours(cursor + 1).ToUnixTimeSeconds(),
                Quantity: (1 + cursor) * 1000,
                Quality: Quality.Measured)).ToList();
    }
}
