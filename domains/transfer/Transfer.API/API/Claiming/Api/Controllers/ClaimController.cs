using System;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.Claiming.Api.Repositories;
using API.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Claiming.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/claims")]
public class ClaimController : ControllerBase
{
    private readonly IClaimRepository claimRepository;

    public ClaimController(IClaimRepository claimRepository)
    {
        this.claimRepository = claimRepository;
    }

    [HttpPost("start-claim-process")]
    [ProducesResponseType(typeof(ClaimSubject), 201)]
    [ProducesResponseType(typeof(ClaimSubject), 200)]
    public async Task<ActionResult> StartClaimProcess()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimRepository.GetClaimSubject(subject);

        if (claim != null)
        {
            return Ok(claim);
        }

        var claimSubject = new ClaimSubject(Guid.Parse(subject));

        try
        {
            claim = await claimRepository.AddClaimSubject(claimSubject);
            return CreatedAtAction(nameof(GetClaimProcess), new { id = claim.SubjectId }, claim);
        }
        catch (DbUpdateException)
        {
            return CreatedAtAction(nameof(GetClaimProcess), new { id = claimSubject.SubjectId }, claimSubject);
        }
    }

    [HttpDelete("stop-claim-process")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> StopClaimProcess()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimRepository.GetClaimSubject(subject);
        if (claim == null)
        {
            return NotFound();
        }

        claimRepository.DeleteClaimSubject(claim);
        return NoContent();
    }

    [HttpGet("claim-process")]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    public async Task<ActionResult<ClaimSubject>> GetClaimProcess()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimRepository.GetClaimSubject(subject);
        if (claim == null)
        {
            return NotFound();
        }

        return Ok(claim);
    }
}
