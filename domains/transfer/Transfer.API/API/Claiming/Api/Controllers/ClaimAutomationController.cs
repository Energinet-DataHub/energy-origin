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
    private readonly IClaimRepository claimRepository;

    public ClaimAutomationController(IClaimRepository claimRepository)
    {
        this.claimRepository = claimRepository;
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(ClaimSubject), 201)]
    [ProducesResponseType(typeof(ClaimSubject), 200)]
    public async Task<ActionResult> StartClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimRepository.GetClaimSubject(Guid.Parse(subject));

        if (claim != null)
        {
            return Ok(claim);
        }

        var claimSubject = new ClaimSubject(Guid.Parse(subject));

        try
        {
            claim = await claimRepository.AddClaimSubject(claimSubject);
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
        var claim = await claimRepository.GetClaimSubject(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        claimRepository.DeleteClaimSubject(claim);
        return NoContent();
    }

    [HttpGet("history")]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(ClaimSubjectHistoryEntriesDto), 200)]
    public async Task<ActionResult<ClaimSubjectHistoryEntriesDto>> GetClaimAutomationHistory()
    {
        var subject = User.FindSubjectGuidClaim();
        var history = await claimRepository.GetHistory(Guid.Parse(subject));

        if (history.Count == 0)
        {
            return NotFound();
        }

        var historyDto = history.Select(c =>
                c.ToDto()
            ).ToList();

        return Ok(new ClaimSubjectHistoryEntriesDto(historyDto));
    }

    [HttpGet]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    public async Task<ActionResult<ClaimSubject>> GetClaimAutomation()
    {
        var subject = User.FindSubjectGuidClaim();
        var claim = await claimRepository.GetClaimSubject(Guid.Parse(subject));
        if (claim == null)
        {
            return NotFound();
        }

        return Ok(claim);
    }
}
