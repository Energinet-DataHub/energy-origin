using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using API.Shared.Extensions;
using API.Transfer.Api.Models;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Repository;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/transfer-agreement-invitations")]
public class TransferAgreementInvitationsController : ControllerBase
{
    private readonly ITransferAgreementInvitationRepository repository;
    private readonly ITransferAgreementRepository transferAgreementRepository;

    public TransferAgreementInvitationsController(ITransferAgreementInvitationRepository repository, ITransferAgreementRepository transferAgreementRepository)
    {
        this.repository = repository;
        this.transferAgreementRepository = transferAgreementRepository;
    }

    /// <summary>
    /// Create transfer agreement invitation
    /// </summary>
    /// <param name="request">The request object containing the StartDate, EndDate and ReceiverTin needed for creating the Transfer Agreement.</param>
    /// <response code="201">Created</response>
    /// <response code="409">There is already a Transfer Agreement with this company tin within the selected date range</response>
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(typeof(string), 409)]
    [HttpPost]
    public async Task<ActionResult> CreateTransferAgreementInvitation(CreateTransferAgreementInvitation request)
    {
        var companySenderId = Guid.Parse(User.FindSubjectGuidClaim());
        var companySenderTin = User.FindSubjectTinClaim();

        var newInvitation = new TransferAgreementInvitation
        {
            SenderCompanyId = companySenderId,
            SenderCompanyTin = companySenderTin,
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = request.ReceiverTin,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = request.EndDate == null ? null : DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value)
        };

        var hasConflict = await transferAgreementRepository.HasDateOverlap(newInvitation);

        if (hasConflict)
        {
            return Conflict("There is already a Transfer Agreement with this company tin within the selected date range");
        }

        await repository.AddTransferAgreementInvitation(newInvitation);

        return CreatedAtAction(nameof(GetTransferAgreementInvitation), new { id = newInvitation.Id }, newInvitation);
    }

    /// <summary>
    /// Get transfer-agreement-invitation by Id
    /// </summary>
    /// <param name="id">Id of transfer-agreement-invitation</param>
    /// <response code="200">Successful operation</response>
    /// <response code="400">You cannot Accept/Deny your own TransferAgreementInvitation</response>
    /// <response code="404">Transfer-agreement-invitation expired or deleted</response>
    [ProducesResponseType(typeof(TransferAgreementInvitation), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult<TransferAgreementInvitation>> GetTransferAgreementInvitation(Guid id)
    {
        var invitation = await repository.GetNonExpiredTransferAgreementInvitation(id);

        if (invitation == null)
        {
            return NotFound("TransferAgreementInvitation expired or deleted");
        }

        var currentCompanyId = Guid.Parse(User.FindSubjectGuidClaim());

        if (currentCompanyId == invitation.SenderCompanyId)
        {
            return BadRequest("You cannot Accept/Deny your own TransferAgreementInvitation");
        }

        return Ok(invitation);
    }
}
