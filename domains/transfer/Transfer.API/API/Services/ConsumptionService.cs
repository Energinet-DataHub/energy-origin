using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;

namespace API.Services;

public class ConsumptionService
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClient;
    private readonly IDataHubFacadeClient _dhFacadeClient;
    private readonly IDataHub3Client _dh3Client;

    public ConsumptionService(Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointClient,
        IDataHubFacadeClient dhFacadeClient,
        IDataHub3Client dh3Client)
    {
        _meteringPointClient = meteringPointClient;
        _dhFacadeClient = dhFacadeClient;
        _dh3Client = dh3Client;
    }

    public async Task<List<ConsumptionHour>> GetTotalHourlyConsumption(OrganizationId organizationId, DateTimeOffset dateFrom, DateTimeOffset dateTo, CancellationToken cancellationToken)
    {
        var mpsRequest = new OwnedMeteringPointsRequest
        {
            Subject = organizationId.Value.ToString()
        };

        var mps = await _meteringPointClient.GetOwnedMeteringPointsAsync(mpsRequest, cancellationToken: cancellationToken);

        var consumptionGsrns = mps.MeteringPoints
            .Where(x => IsConsumption(x.TypeOfMp))
            .Select(x => new Gsrn(x.MeteringPointId))
            .ToList();

        var mpRelations = await _dhFacadeClient.ListCustomerRelations(organizationId.Value.ToString(), consumptionGsrns, cancellationToken);
        if (mpRelations == null)
            return new List<ConsumptionHour>();

        var validRelations = mpRelations.Relations.Where(x => x.IsValid()).ToList();

        var totalConsumption = await _dh3Client.GetMeasurements(validRelations.Select(x => new Gsrn(x.MeteringPointId)).ToList(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), cancellationToken);

        if (totalConsumption == null)
            return new List<ConsumptionHour>();

        return MapToTotalHourFormat(totalConsumption);
    }

    private static bool IsConsumption(string typeOfMp)
    {
        return typeOfMp.Trim().ToUpper() == "E17";
    }

    public List<ConsumptionHour> MapToTotalHourFormat(MeteringPointData[] totalConsumption)
    {
        var result = Enumerable.Range(0, 24).Select(x => new ConsumptionHour(x)).ToList();

        foreach (var mp in totalConsumption)
        {
            foreach (var day in mp.PointAggregationGroups)
            {
                foreach (var entry in day.Value.PointAggregations)
                {
                    var hour = DateTimeOffset.FromUnixTimeSeconds(entry.MinObservationTime).Hour;

                    result.First(x => x.HourOfDay == hour).KwhQuantity += entry.AggregatedQuantity;
                }
            }
        }

        return result;
    }
}

public record ConsumptionHour(int HourOfDay)
{
    public decimal KwhQuantity { get; set; }
}
