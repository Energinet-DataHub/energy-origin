using System;
using System.Linq;
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
    [ProducesResponseType(typeof(ClaimAutomationArgument), 201)]
    [ProducesResponseType(typeof(ClaimAutomationArgument), 200)]
    public async Task<ActionResult> StartClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimSubject(Guid.Parse(subject));

        if (claim != null)
        {
            return Ok(claim);
        }

        var claimSubject = new ClaimAutomationArgument(Guid.Parse(subject), DateTimeOffset.UtcNow);

        try
        {
            claim = await claimAutomationRepository.AddClaimSubject(claimSubject);
            return CreatedAtAction(nameof(GetClaimAutomation), null, claim);
        }
        catch (DbUpdateException)
        {
            return CreatedAtAction(nameof(GetClaimAutomation), null, claimSubject);
        }
    }

    [HttpDelete("stop")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
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
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    public async Task<ActionResult<ClaimAutomationArgumentDto>> GetClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimAutomationRepository.GetClaimSubject(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        var claimSubjectDto = new ClaimAutomationArgumentDto(claim.SubjectId, claim.CreatedAt);

        return Ok(claimSubjectDto);
    }
}
