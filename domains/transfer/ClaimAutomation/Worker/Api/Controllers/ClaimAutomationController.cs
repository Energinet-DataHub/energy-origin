using System;
using System.Threading.Tasks;
using Asp.Versioning;
using ClaimAutomation.Worker.Api.Dto.Response;
using ClaimAutomation.Worker.Api.Repositories;
using DataContext.Models;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimAutomation.Worker.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/claim-automation")]
public class ClaimAutomationController(IClaimAutomationRepository claimAutomationRepository) : ControllerBase
{
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpPost("start")]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 201)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult> StartClaimAutomation()
    {
        var user = new UserDescriptor(User);

        var claim = await claimAutomationRepository.GetClaimAutomationArgument(user.Subject);

        if (claim != null)
        {
            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

            return Ok(claimAutomationArgumentDto);
        }

        var claimAutomationArgument = new ClaimAutomationArgument(user.Subject, DateTimeOffset.UtcNow);

        try
        {
            claim = await claimAutomationRepository.AddClaimAutomationArgument(claimAutomationArgument);
            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

            return CreatedAtAction(nameof(GetClaimAutomation), null, claimAutomationArgumentDto);
        }
        catch (DbUpdateException)
        {
            var claimAutomation = await claimAutomationRepository.GetClaimAutomationArgument(user.Subject);

            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claimAutomation!.CreatedAt.ToUnixTimeSeconds());
            return Ok(claimAutomationArgumentDto);
        }
    }
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpDelete("stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> StopClaimAutomation()
    {
        var user = new UserDescriptor(User);

        var claim = await claimAutomationRepository.GetClaimAutomationArgument(user.Subject);

        if (claim == null)
        {
            return NotFound();
        }

        await claimAutomationRepository.DeleteClaimAutomationArgument(claim);
        return NoContent();
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpGet]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult<ClaimAutomationArgumentDto>> GetClaimAutomation()
    {
        var user = new UserDescriptor(User);

        var claim = await claimAutomationRepository.GetClaimAutomationArgument(user.Subject);

        if (claim == null)
        {
            return NotFound();
        }

        var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt.ToUnixTimeSeconds());

        return Ok(claimAutomationArgumentDto);
    }
}
