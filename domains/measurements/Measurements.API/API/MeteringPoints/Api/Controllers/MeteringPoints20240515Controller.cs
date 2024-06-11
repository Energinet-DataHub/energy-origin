using System;
using System.Linq;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Dto.Responses.Enums;
using API.MeteringPoints.Api.Models;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Meteringpoint.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeteringPoint = API.MeteringPoints.Api.Dto.Responses.MeteringPoint;

namespace API.MeteringPoints.Api.Controllers;

[Authorize(Policy.B2CPolicy)]
[ApiController]
[ApiVersion(ApiVersions.Version20240515)]
[Route("api/measurements/meteringpoints")]
public class MeteringPoints20240515Controller : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;
    private readonly ApplicationDbContext _dbContext;

    public MeteringPoints20240515Controller(Meteringpoint.V1.Meteringpoint.MeteringpointClient client, ApplicationDbContext dbContext)
    {
        _client = client;
        _dbContext = dbContext;
    }


    /// <summary>
    /// Get metering points from DataHub2.0
    /// </summary>
    /// <response code="200">Successful operation</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetMeteringPointsResponse), 200)]
    public async Task<ActionResult> GetMeteringPoints([FromQuery] Guid orgId)
    {
        var identity = new IdentityDescriptor(HttpContext, orgId);

        var request = new OwnedMeteringPointsRequest
        {
            Subject = orgId.ToString(),
            Actor = identity.Sub.ToString()
        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.Child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        var relation = _dbContext.Relations.FirstOrDefault(u => u.SubjectId == orgId);
        var status = relation?.Status ?? (meteringPoints.Count != 0 ? RelationStatus.Created : RelationStatus.Pending);

        return Ok(new GetMeteringPointsResponse(meteringPoints, status));
    }
}
