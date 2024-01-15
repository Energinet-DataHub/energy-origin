using System.Linq;
using System.Threading.Tasks;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Enums;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.MeteringPoints.Api.v2024_01_10.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20240110")]
[Route("api/meteringpoints")]
public class MeteringPointsController : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;

    public MeteringPointsController(Meteringpoint.V1.Meteringpoint.MeteringpointClient client)
    {
        _client = client;
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpGet]
    public async Task<GetMeteringPointsResponse> GetMeteringPoints()
    {
        var user = new UserDescriptor(User);

        var request = new Meteringpoint.V1.OwnedMeteringPointsRequest
        {
            Subject = user.Subject.ToString(),
            Actor = user.Id.ToString()
        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        return new GetMeteringPointsResponse(meteringPoints);
    }
}
