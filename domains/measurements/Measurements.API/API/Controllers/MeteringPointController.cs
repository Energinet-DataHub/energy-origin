using API.Models;
using API.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class MeteringPointController : Controller
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;

    public MeteringPointController(Meteringpoint.V1.Meteringpoint.MeteringpointClient client)
    {
        _client = client;
    }

    public async Task<List<MeteringPoint>> Index()
    {
        var request = new Meteringpoint.V1.OwnedMeteringPointsRequest
        {

        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        return meteringPoints;
    }
}
