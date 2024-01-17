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

    /// <summary>
    /// Get metering points from DataHub2.0
    /// </summary>
    /// <response code="200">Successful operation</response>
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpGet]
    [ProducesResponseType(typeof(GetMeteringPointsResponse), 200)]
    public async Task<ActionResult> GetMeteringPoints()
    {
        var user = new UserDescriptor(User);

        var request = new Meteringpoint.V1.OwnedMeteringPointsRequest
        {
            Subject = user.Subject.ToString(),
            Actor = user.Id.ToString()
        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.Child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        return Ok(new GetMeteringPointsResponse(meteringPoints));
    }
}
