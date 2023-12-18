using System;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.Claiming.Api.Repositories;
using API.Claiming.Api.v2023_01_01.Dto.Response;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Claiming.Api.v2023_01_01.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101")]
[Route("api/claim-automation")]
public class ClaimAutomationController : ControllerBase
{
    private readonly IClaimAutomationRepository claimAutomationRepository;

    public ClaimAutomationController(IClaimAutomationRepository claimAutomationRepository)
    {
        this.claimAutomationRepository = claimAutomationRepository;
    }

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
