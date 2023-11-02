using System;
using System.Threading.Tasks;
using API.Claiming.Api.Dto.Response;
using API.Claiming.Api.Models;
using API.Claiming.Api.Repositories;
using API.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Claiming.Api.Controllers;

[Authorize]
[ApiController]
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
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimAutomationArgument(Guid.Parse(subject));
        if (claim != null)
        {
            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt);

            return Ok(claimAutomationArgumentDto);
        }

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.Parse(subject), DateTimeOffset.UtcNow);

        try
        {
            claim = await claimAutomationRepository.AddClaimAutomationArgument(claimAutomationArgument);
            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt);

            return CreatedAtAction(nameof(GetClaimAutomation), null, claimAutomationArgumentDto);
        }
        catch (DbUpdateException)
        {
            var claimAutomation = await claimAutomationRepository.GetClaimAutomationArgument(Guid.Parse(subject));

            var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claimAutomation!.CreatedAt);
            return Ok(claimAutomationArgumentDto);
        }
    }

    [HttpDelete("stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> StopClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimAutomationArgument(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        claimAutomationRepository.DeleteClaimAutomationArgument(claim);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult<ClaimAutomationArgumentDto>> GetClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimAutomationArgument(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        var claimAutomationArgumentDto = new ClaimAutomationArgumentDto(claim.CreatedAt);

        return Ok(claimAutomationArgumentDto);
    }
}
