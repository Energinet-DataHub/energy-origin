using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.ApiModels.Requests;
using API.Services;

[ApiController]
[Route("api/[controller]")]
public class TransferAgreementController : ControllerBase
{
    private readonly ITransferAgreementService transferAgreementService;

    public TransferAgreementController(ITransferAgreementService transferAgreementService)
    {
        this.transferAgreementService = transferAgreementService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferAgreementRequestModel transferAgreementRequest)
    {
        try
        {
            var loggedInParticipantId = GetLoggedInParticipantId();

            if (transferAgreementRequest.ProviderId == transferAgreementRequest.ReceiverId)
            {
                return BadRequest("A participant cannot send a transfer agreement to themselves.");
            }

            if (transferAgreementRequest.ProviderId != loggedInParticipantId)
            {
                return BadRequest("The provider ID in the request does not match the logged-in participant ID.");
            }

            transferAgreementRequest.Id = Guid.NewGuid(); // Generate a new ID for the transfer agreement
            var transferAgreement = await transferAgreementService.CreateTransferAgreementAsync(transferAgreementRequest);
            return CreatedAtAction(nameof(GetById), new { id = transferAgreement.Id }, transferAgreement);
        }
        catch (Exception ex)
        {
            // Handle the exception based on your project's error handling strategy
            return BadRequest(ex.Message);
        }
    }



    [HttpPost("{id}/change")]
public async Task<IActionResult> Update(Guid id, [FromBody] TransferAgreementChangeRequestModel changeRequest)
{
    try
    {
        var loggedInParticipantId = GetLoggedInParticipantId();
        var existingTransferAgreement = await transferAgreementService.GetTransferAgreementByIdAsync(id, loggedInParticipantId);

        if (existingTransferAgreement == null)
        {
            return NotFound();
        }

        if (changeRequest.StartDate.HasValue && changeRequest.StartDate.Value < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return BadRequest("Start date cannot be set to a date prior to the current time.");
        }

        if (changeRequest.EndDate.HasValue && changeRequest.EndDate.Value < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return BadRequest("End date cannot be set to a date prior to the current time.");
        }

        var updatedTransferAgreement = await transferAgreementService.UpdateTransferAgreementAsync(id, changeRequest);
        return Ok(updatedTransferAgreement);
    }
    catch (Exception ex)
    {
        // Handle the exception based on your project's error handling strategy
        return BadRequest(ex.Message);
    }
}


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var loggedInParticipantId = GetLoggedInParticipantId();
            var transferAgreement = await transferAgreementService.GetTransferAgreementByIdAsync(id, loggedInParticipantId);

            if (transferAgreement == null)
            {
                return NotFound();
            }

            return Ok(transferAgreement);
        }
        catch (Exception ex)
        {
            // Handle the exception based on your project's error handling strategy
            return BadRequest(ex.Message);
        }
    }

    private Guid GetLoggedInParticipantId()
    {
        if (HttpContext.Session.TryGetValue("participantId", out var participantIdBytes))
        {
            return new Guid(participantIdBytes);
        }
        else
        {
            throw new InvalidOperationException("Participant ID not found in session.");
        }
    }
}
