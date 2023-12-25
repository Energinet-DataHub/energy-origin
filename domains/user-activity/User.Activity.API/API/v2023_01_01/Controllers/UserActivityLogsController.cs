using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Models;
using System.Linq;
using API.Repository;
using API.v2023_01_01.Dto.Requests;
using API.v2023_01_01.Dto.Responses;
using API.v2023_01_01.Extensions;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;

namespace API.v2023_01_01.Controllers;

[ApiController]
[ApiVersion("20230101")]
[Route("api/user-activity-logs")]
public class UserActivityLogsController(IUserActivityLogsRepository repository) : ControllerBase
{
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(UserActivityLogsResponse), 200)]
    [HttpGet]
    public async Task<ActionResult<UserActivityLogsResponse>> GetUserActivityLogs([FromQuery] GetUserActivityLogsRequestDto requestDto)
    {
        var user = new UserDescriptor(User);
        var actorId = user.Subject;

        var startDate = requestDto.StartDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(requestDto.StartDate.Value) : (DateTimeOffset?)null;
        var endDate = requestDto.EndDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(requestDto.EndDate.Value) : (DateTimeOffset?)null;

        var result = await repository.GetUserActivityLogsAsync(
            actorId: actorId,
            entityTypes: requestDto.EntityTypes,
            startDate: startDate,
            endDate: endDate,
            pagination: new Pagination(requestDto.Offset, requestDto.Limit));

        var response = new UserActivityLogsResponse(
            TotalCount: result.TotalCount,
            Items: result.Items.Select(log => log.ToDto()).ToList());

        return Ok(response);
    }
}
