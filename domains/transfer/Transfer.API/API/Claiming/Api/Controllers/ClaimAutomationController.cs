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
        var claim = await claimAutomationRepository.GetClaimSubject(Guid.Parse(subject));
        if (claim != null)
        {
            var claimSubjectDto = new ClaimAutomationArgumentDto(claim.CreatedAt);

            return Ok(claimSubjectDto);
        }

        var claimSubject = new ClaimAutomationArgument(Guid.Parse(subject), DateTimeOffset.UtcNow);

        try
        {
            claim = await claimAutomationRepository.AddClaimSubject(claimSubject);
            var claimSubjectDto = new ClaimAutomationArgumentDto(claim.CreatedAt);

            return CreatedAtAction(nameof(GetClaimAutomation), null, claimSubjectDto);
        }
        catch (DbUpdateException)
        {
            var claimAutomation = await claimAutomationRepository.GetClaimSubject(Guid.Parse(subject));

            var claimSubjectDto = new ClaimAutomationArgumentDto(claimAutomation!.CreatedAt);
            return Ok(claimSubjectDto);
        }
    }

    [HttpDelete("stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> StopClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimSubject(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        claimAutomationRepository.DeleteClaimSubject(claim);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ClaimAutomationArgumentDto), 200)]
    public async Task<ActionResult<ClaimAutomationArgumentDto>> GetClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimSubject(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        var claimSubjectDto = new ClaimAutomationArgumentDto(claim.CreatedAt);

        return Ok(claimSubjectDto);
    }
}
