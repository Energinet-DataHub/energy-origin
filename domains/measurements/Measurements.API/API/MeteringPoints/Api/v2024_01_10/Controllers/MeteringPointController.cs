using API.MeteringPoints.Api.v2024_01_10.Dto.Responses;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Enums;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.MeteringPoints.Api.v2024_01_10.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20240110")]
[Route("api/meteringpoints")]
public class MeteringPointController : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;

    public MeteringPointController(Meteringpoint.V1.Meteringpoint.MeteringpointClient client)
    {
        _client = client;
    }

    public async Task<GetMeteringPointsResponse> GetMeteringPoints()
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

        return new GetMeteringPointsResponse(meteringPoints);
    }
}
