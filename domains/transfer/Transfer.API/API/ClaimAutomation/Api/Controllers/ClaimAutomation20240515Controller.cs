using System;
using System.Threading.Tasks;
using API.ClaimAutomation.Api.Dto.Response;
using API.ClaimAutomation.Api.Repositories;
using API.Transfer.Api.Controllers;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.ClaimAutomation.Api.Controllers;

[ApiController]
[Authorize(Policy.Frontend)]
[ApiVersion(ApiVersions.Version20240515)]
[Route("api/claim-automation")]
public class ClaimAutomation20240515Controller(IUnitOfWork unitOfWork, AccessDescriptor accessDescriptor) : ControllerBase
{
    [HttpPost("start")]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 201)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult> StartClaimAutomation([FromQuery] Guid organizationId)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

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

    [HttpDelete("stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> StopClaimAutomation([FromQuery] Guid organizationId)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

        var claim = await unitOfWork.ClaimAutomationRepository.GetClaimAutomationArgument(organizationId);

        if (claim == null)
        {
            return NotFound();
        }

        await unitOfWork.ClaimAutomationRepository.DeleteClaimAutomationArgument(claim);
        return NoContent();
    }


    [HttpGet]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult<ClaimAutomationArgumentDto>> GetClaimAutomation([FromQuery] Guid organizationId)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

        var claim = await unitOfWork.ClaimAutomationRepository.GetClaimAutomationArgument(organizationId);

        if (claim == null)
        {
            return NotFound();
        }

        var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

        return Ok(claimAutomationArgumentDto);
    }
}
