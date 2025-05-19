using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnergyOrigin.Datahub3;
using EnergyOrigin.Domain.ValueObjects;
using MeteringPoint = EnergyOrigin.Datahub3.MeteringPoint;

namespace EnergyTrackAndTrace.Testing;

public class Any
{
    public static MeteringPointData[] TimeSeriesApiResponse(Gsrn gsrn, long dateFrom, long dateTo, long initValue)
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
