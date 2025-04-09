using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

namespace API.MeteringPoints.Api.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.AdminPortal)]
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/measurements/admin-portal")]
public class InternalMeteringpointsController : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;

    public InternalMeteringpointsController(Meteringpoint.V1.Meteringpoint.MeteringpointClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Get as a post. Get metering points from DataHub2.0
    /// </summary>
    /// <response code="200">Successful operation</response>[ApiController]
    [HttpPost]
    [Route("internal-meteringpoints")]
    [ProducesResponseType(typeof(GetMeteringPointsResponse), 200)]
    public async Task<ActionResult> GetMeteringPoints([Required][FromBody] List<Guid> organizationIds)
    {
        var allMeteringPoints = new List<MeteringPoint>();

        foreach (Guid organizationId in organizationIds)
        {
            var request = new OwnedMeteringPointsRequest
            {
                Subject = organizationId.ToString(),
                Actor = "Ett-admin-portal"
            };
            var response = await _client.GetOwnedMeteringPointsAsync(request);

            var meteringPoints = response.MeteringPoints
                .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.Child)
                .Select(MeteringPoint.CreateFrom)
                .ToList();

            allMeteringPoints.AddRange(meteringPoints);
        }

        return Ok(new GetInternalMeteringPointsResponse(allMeteringPoints));
    }

}

public record GetInternalMeteringPointsResponse(List<MeteringPoint> Result);

