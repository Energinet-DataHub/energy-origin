using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Datahub3;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;
using MeteringPoint = EnergyOrigin.Datahub3.MeteringPoint;

namespace API.UnitTests;

public class Any
{
    public static MeteringPointsResponse MeteringPointsResponse(Gsrn gsrn)
    {
        return new MeteringPointsResponse { MeteringPoints = { MeteringPoint(gsrn) } };
    }

    public static MeteringPointData[] TimeSeriesApiResponse(Gsrn gsrn, long dateFrom, long dateTo, long initValue)
    {
        var dateFromDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(dateFrom);
        var dateToDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(dateTo);

        var totalDays = (dateFromDateTimeOffset - dateToDateTimeOffset).Days + 1;
        List<DateTimeOffset> daysRange = Enumerable.Range(0, totalDays)
            .Select(i => dateFromDateTimeOffset.AddDays(i))
            .ToList();

        var pags = new Dictionary<string, PointAggregationGroup>();
        foreach (var day in daysRange)
        {
            var pag = new PointAggregationGroup
            {
                MinObservationTime = day.ToUnixTimeSeconds(),
                MaxObservationTime = day.AddDays(1).ToUnixTimeSeconds(),
                Resolution = "PT1H",
                PointAggregations = new List<PointAggregation>()
            };

            for (int i = 0; i < 24; i++)
            {
                var pa = new PointAggregation
                {
                    MinObservationTime = day.AddHours(i).ToUnixTimeSeconds(),
                    MaxObservationTime = day.AddHours(i + 1).ToUnixTimeSeconds(),
                    AggregatedQuantity = initValue + i,
                    Quality = "measured"
                };
                pag.PointAggregations.Add(pa);
            }

            pags.Add(gsrn.Value + "_" + day.ToString("yyyy-MM-dd"), pag);
        }

        return [new MeteringPointData
        {
            MeteringPoint = new MeteringPoint { Id = gsrn.Value },
            PointAggregationGroups = pags
        }];
    }

    public static MeteringPointData[] TimeSeriesApiResponse(Gsrn gsrn, List<PointAggregation> pas)
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

            var pag = new PointAggregationGroup
            {
                MinObservationTime = buffer.Min(x => x.MinObservationTime),
                MaxObservationTime = buffer.Max(x => x.MaxObservationTime),
                Resolution = "PT1H",
                PointAggregations = buffer.ToList()
            };
            pags.Add(gsrn.Value + "_" + UnixTimestamp.Create(pag.MinObservationTime).ToDateTimeOffset().ToString("yyyy-MM-dd"), pag);
        }

        return [new MeteringPointData
        {
            MeteringPoint = new MeteringPoint { Id = gsrn.Value },
            PointAggregationGroups = pags
        }];
    }

    public static PointAggregation PointAggregation(long minObservationTime, decimal aggregatedQuantity)
    {
        return new PointAggregation
        {
            MinObservationTime = minObservationTime,
            MaxObservationTime = minObservationTime,
            AggregatedQuantity = aggregatedQuantity,
            Quality = "measured"
        };
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
            MeteringGridAreaId = "932"
        };
    }

    public static Gsrn Gsrn()
    {
        return new Gsrn("57" + IntString(16));
    }

    private static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }

    public static Technology Technology()
    {
        return new Technology("T12345", "T54321");
    }

    public static Measurement Measurement(Gsrn gsrn, long dateFrom, long value)
    {
        return new Measurement
        {
            Gsrn = gsrn.Value,
            DateFrom = dateFrom,
            DateTo = dateFrom + 3600,
            Quantity = value,
            Quality = EnergyQuality.Measured
        };
    }

    public static CertificateIssuingContract CertificateIssuingContract(Gsrn gsrn, UnixTimestamp start, UnixTimestamp? end, int contractNumber = 0)
    {
        return new CertificateIssuingContract()
        {
            GSRN = gsrn.Value,
            StartDate = start.ToDateTimeOffset(),
            EndDate = end?.ToDateTimeOffset(),
            ContractNumber = contractNumber
        };
    }

    public static MeteringPointTimeSeriesSlidingWindow MeteringPointTimeSeriesSlidingWindow(
        Gsrn? gsrn = null,
        UnixTimestamp? syncPoint = null,
        List<MeasurementInterval>? intervals = null)
    {
        return DataContext.Models.MeteringPointTimeSeriesSlidingWindow.Create(
            gsrn ?? Gsrn(),
            syncPoint ?? UnixTimestamp.Create(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            intervals ?? new List<MeasurementInterval>()
        );
    }
}
