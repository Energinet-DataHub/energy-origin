using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Dto.Responses.Enums;
using API.MeteringPoints.Api.Models;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using Meteringpoint.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeteringPoint = API.MeteringPoints.Api.Dto.Responses.MeteringPoint;

namespace API.MeteringPoints.Api.Controllers;

[Authorize(Policy.FrontendOr3rdParty)]
[ApiController]
[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
[Route("api/measurements/meteringpoints")]
public class MeteringPoints20240515Controller : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly AccessDescriptor _accessDescriptor;

    public MeteringPoints20240515Controller(Meteringpoint.V1.Meteringpoint.MeteringpointClient client, ApplicationDbContext dbContext,
        IdentityDescriptor identityDescriptor, AccessDescriptor accessDescriptor)
    {
        _client = client;
        _dbContext = dbContext;
        _identityDescriptor = identityDescriptor;
        _accessDescriptor = accessDescriptor;
    }


    /// <summary>
    /// Get metering points from DataHub2.0
    /// </summary>
    /// <response code="200">Successful operation</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetMeteringPointsResponse), 200)]
    public async Task<ActionResult> GetMeteringPoints([Required][FromQuery] Guid organizationId)
    {
        if (!_accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

        var request = new OwnedMeteringPointsRequest
        {
            Subject = organizationId.ToString(),
            Actor = _identityDescriptor.Subject.ToString()
        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.Child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        var relation = _dbContext.Relations.FirstOrDefault(u => u.SubjectId == organizationId);
        var status = relation?.Status ?? (meteringPoints.Count != 0 ? RelationStatus.Created : RelationStatus.Pending);

        return Ok(new GetMeteringPointsResponse(meteringPoints, status));
    }
}
