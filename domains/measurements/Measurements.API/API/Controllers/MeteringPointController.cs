using API.Models;
using API.Models.Enums;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/meteringpoints")]
public class MeteringPointController : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;

    public MeteringPointController(Meteringpoint.V1.Meteringpoint.MeteringpointClient client)
    {
        _client = client;
    }

    public async Task<List<MeteringPoint>> GetMeteringPoints()
    {
        var user = new UserDescriptor(User);

        var request = new Meteringpoint.V1.OwnedMeteringPointsRequest
        {
            Subject = user.Subject.ToString(),
            Actor = user.Name //TODO maybe this is not correct
        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        return meteringPoints;
    }
}
