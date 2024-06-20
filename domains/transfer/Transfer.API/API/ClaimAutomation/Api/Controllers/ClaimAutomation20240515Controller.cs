using System;
using System.Threading.Tasks;
using API.ClaimAutomation.Api.Dto.Response;
using API.ClaimAutomation.Api.Repositories;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.ClaimAutomation.Api.Controllers;

[Authorize]
[ApiController]
[Authorize(Policy.B2CCvrClaim)]
[ApiVersion(ApiVersions.Version20300101)]
[Route("api/claim-automation")]
public class ClaimAutomationController20300101(IUnitOfWork unitOfWork) : ControllerBase
{
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpPost("start")]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 201)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult> StartClaimAutomation([FromQuery] Guid organizationId)
    {
        var claim = await unitOfWork.ClaimAutomationRepository.GetClaimAutomationArgument(organizationId);

        if (claim != null)
        {
            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

            return Ok(claimAutomationArgumentDto);
        }

        var claimAutomationArgument = new ClaimAutomationArgument(organizationId, DateTimeOffset.UtcNow);

        try
        {
            claim = await unitOfWork.ClaimAutomationRepository.AddClaimAutomationArgument(claimAutomationArgument);
            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

            return CreatedAtAction(nameof(GetClaimAutomation), null, claimAutomationArgumentDto);
        }
        catch (DbUpdateException)
        {
            var claimAutomation = await unitOfWork.ClaimAutomationRepository.GetClaimAutomationArgument(organizationId);

            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claimAutomation!.CreatedAt.ToUnixTimeSeconds());
            return Ok(claimAutomationArgumentDto);
        }
    }
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpDelete("stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> StopClaimAutomation([FromQuery] Guid organizationId)
    {
        var claim = await unitOfWork.ClaimAutomationRepository.GetClaimAutomationArgument(organizationId);

        if (claim == null)
        {
            return NotFound();
        }

        await unitOfWork.ClaimAutomationRepository.DeleteClaimAutomationArgument(claim);
        return NoContent();
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpGet]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult<ClaimAutomationArgumentDto>> GetClaimAutomation([FromQuery] Guid organizationId)
    {
        var claim = await unitOfWork.ClaimAutomationRepository.GetClaimAutomationArgument(organizationId);

        if (claim == null)
        {
            return NotFound();
        }

        var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

        return Ok(claimAutomationArgumentDto);
    }
}
