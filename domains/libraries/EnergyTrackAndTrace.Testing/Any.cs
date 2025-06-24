using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Energinet.DataHub.Measurements.Abstractions.Api.Models;
using EnergyOrigin.Domain.ValueObjects;
using NodaTime;

namespace EnergyTrackAndTrace.Testing;

public class Any
{
    public static MeasurementAggregationByPeriodDto[] MeasurementsApiResponse(Gsrn gsrn, long dateFrom, long dateTo, long initValue, int? quantity = null)
    {
        var dateFromDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(dateFrom);
        var dateToDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(dateTo);

        var totalDays = (dateToDateTimeOffset - dateFromDateTimeOffset).Days;
        if (totalDays == 0 && (dateToDateTimeOffset - dateFromDateTimeOffset).Hours > 0)
        {
            totalDays = 1;
        }

        List<DateTimeOffset> daysRange = Enumerable.Range(0, totalDays)
            .Select(i => dateFromDateTimeOffset.AddDays(i))
            .ToList();

        var pags = new Dictionary<string, PointAggregationGroup>();
        foreach (var day in daysRange)
        {
            var pag = new PointAggregationGroup(
                    Instant.FromUnixTimeSeconds(day.ToUnixTimeSeconds()),
                    Instant.FromUnixTimeSeconds(day.AddDays(1).ToUnixTimeSeconds()),
                    Resolution.Hourly,
                    []);

            for (int i = 0; i < 24; i++)
            {
                var pa = new PointAggregation(
                        Instant.FromUnixTimeSeconds(day.AddHours(i).ToUnixTimeSeconds()),
                        Instant.FromUnixTimeSeconds(day.AddHours(i + 1).ToUnixTimeSeconds()),
                        quantity is null ? initValue + i : quantity,
                        Quality.Measured);

                pag.PointAggregations.Add(pa);
            }

            pags.Add(gsrn.Value + "_" + day.ToString("yyyy-MM-dd"), pag);
        }

        return [new MeasurementAggregationByPeriodDto(new MeteringPoint(gsrn.Value), pags)];
    }


    public static MeasurementAggregationByPeriodDto[] DH3MeasurementsApiResponse(Gsrn gsrn, List<PointAggregation> pas)
    {
        var pags = new Dictionary<string, PointAggregationGroup>();

        var pasArray = pas.ToArray();
        int totalChunks = (int)Math.Ceiling((double)pas.Count / 24);
        for (int i = 0; i < totalChunks; i++)
        {
            int startIndex = i * 24;
            int chunkSize = Math.Min(24, pas.Count - startIndex);

            var buffer = new PointAggregation[chunkSize];
            Array.Copy(pasArray, startIndex, buffer, 0, chunkSize);

            var pag = new PointAggregationGroup(
                    buffer.Min(x => x.From),
                    buffer.Max(x => x.To),
                    Resolution.Hourly,
                    [.. buffer]);

            pags.Add(
                    gsrn.Value + "_" + UnixTimestamp.Create(pag.From.ToUnixTimeSeconds()).ToDateTimeOffset().ToString("yyyy-MM-dd"),
                    pag);
        }

        return [new MeasurementAggregationByPeriodDto(
               new MeteringPoint(gsrn.Value),
               pags )];
    }

    public static Gsrn Gsrn()
    {
        var rand = new Random();
        var sb = new StringBuilder();
        sb.Append("57");
        for (var i = 0; i < 16; i++)
        {
            sb.Append(rand.Next(0, 9));
        }

        return new Gsrn(sb.ToString());
    }

    public static Tin Tin()
    {
        return EnergyOrigin.Domain.ValueObjects.Tin.Create(IntString(8));
    }

    private static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }

    public static PointAggregation PointAggregation(long minObservationTime, decimal aggregatedQuantity)
    {
        return new PointAggregation
        (
            Instant.FromUnixTimeSeconds(minObservationTime),
            Instant.FromUnixTimeSeconds(minObservationTime),
            aggregatedQuantity,
            Quality.Measured
        );
    }

    public static PointAggregation PointAggregation(long minObservationTime, long maxObservationTime, decimal aggregatedQuantity)
    {
        return new PointAggregation
        (
            Instant.FromUnixTimeSeconds(minObservationTime),
            Instant.FromUnixTimeSeconds(maxObservationTime),
            aggregatedQuantity,
            Quality.Measured
        );
    }

    public static Meteringpoint.V1.MeteringPoint MeteringPoint(Gsrn gsrn)
    {
        return new Meteringpoint.V1.MeteringPoint
        {
            MeteringPointId = gsrn.Value,
            MeteringPointAlias = "alias",
            ConsumerStartDate = "consumerStartDate",
            Capacity = "123",
            BuildingNumber = "buildingNumber",
            CityName = "cityName",
            Postcode = "8240",
            StreetName = "streetName",
            MunicipalityCode = "101", // Copenhagen
            CitySubDivisionName = "vesterbro",
            MeteringGridAreaId = "932",
            PhysicalStatusOfMp = "E22"
        };
    }

    public static Meteringpoint.V1.MeteringPoint ConsumptionMeteringPoint(Gsrn gsrn)
    {
        return new Meteringpoint.V1.MeteringPoint
        {
            MeteringGridAreaId = "932",
            Postcode = "8240",
            AssetType = "D12", //Wind
            BuildingNumber = "1",
            Capacity = "1234",
            CityName = "Some city",
            CitySubDivisionName = "Some sub division name",
            ConsumerCvr = "12345678",
            ConsumerStartDate = "Some date",
            DataAccessCvr = "12345678",
            FloorId = "1",
            MeteringPointAlias = "Some alias",
            MeteringPointId = gsrn.Value,
            TypeOfMp = "E17", //Consumption
            MunicipalityCode = "101", //Copenhagen
            PhysicalStatusOfMp = "E22",
            RoomId = "1",
            StreetName = "Some street",
            SubtypeOfMp = "D01", //Physical
            WebAccessCode = "Some web access code"
        };
    }
}
