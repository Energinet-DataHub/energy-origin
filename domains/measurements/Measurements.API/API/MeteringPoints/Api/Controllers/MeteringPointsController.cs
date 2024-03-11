using System.Linq;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Dto.Responses.Enums;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.MeteringPoints.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240110)]
[Route("api/measurements/meteringpoints")]
public class MeteringPointsController : ControllerBase
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _client;
    private readonly ApplicationDbContext _dbContext;

    public MeteringPointsController(Meteringpoint.V1.Meteringpoint.MeteringpointClient client,
        ApplicationDbContext dbContext)
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
    public async Task<ActionResult> GetMeteringPoints()
    {
        var user = new UserDescriptor(User);

        var request = new Meteringpoint.V1.OwnedMeteringPointsRequest
        {
            Subject = user.Subject.ToString(),
            Actor = user.Id.ToString()
        };
        var response = await _client.GetOwnedMeteringPointsAsync(request);

        var relation = _dbContext.Relations.SingleOrDefault(u => u.SubjectId == user.Subject);

        var meteringPoints = response.MeteringPoints
            .Where(mp => MeteringPoint.GetMeterType(mp.TypeOfMp) != MeterType.Child)
            .Select(MeteringPoint.CreateFrom)
            .ToList();

        return Ok(new GetMeteringPointsResponse(meteringPoints, relation?.Status));
    }
}
