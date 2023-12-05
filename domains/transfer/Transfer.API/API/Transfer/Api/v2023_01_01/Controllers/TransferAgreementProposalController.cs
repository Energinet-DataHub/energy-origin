using System;
using System.Threading.Tasks;
using API.Shared.Extensions;
using API.Transfer.Api.Models;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_01_01.Dto.Requests;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2023_01_01.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101")]
[Route("api/transfer-agreement-proposals")]
public class TransferAgreementProposalController : ControllerBase
{
    private readonly ITransferAgreementProposalRepository repository;
    private readonly IValidator<CreateTransferAgreementProposal> createTransferAgreementProposalValidator;
    private readonly ITransferAgreementRepository transferAgreementRepository;

    public TransferAgreementProposalController(ITransferAgreementProposalRepository repository,
        IValidator<CreateTransferAgreementProposal> createTransferAgreementProposalValidator,
        ITransferAgreementRepository transferAgreementRepository)
    {
        this.repository = repository;
        this.createTransferAgreementProposalValidator = createTransferAgreementProposalValidator;
        this.transferAgreementRepository = transferAgreementRepository;
    }

    /// <summary>
    /// Create TransferAgreementProposal
    /// </summary>
    /// <param name="request">The request object containing the StartDate, EndDate and ReceiverTin needed for creating the Transfer Agreement.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad request</response>
    /// <response code="409">There is already a Transfer Agreement with this company tin within the selected date range</response>
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(string), 409)]
    [HttpPost]
    public async Task<ActionResult> CreateTransferAgreementProposal(CreateTransferAgreementProposal request)
    {
        var companySenderId = Guid.Parse(User.FindSubjectGuidClaim());
        var companySenderTin = User.FindSubjectTinClaim();
        var companySenderName = User.FindSubjectNameClaim();

        var validateResult = await createTransferAgreementProposalValidator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var newProposal = new TransferAgreementProposal
        {
            SenderCompanyId = companySenderId,
            SenderCompanyTin = companySenderTin,
            SenderCompanyName = companySenderName,
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = request.ReceiverTin,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = request.EndDate == null ? null : DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value)
        };

        if (request.ReceiverTin != null)
        {
            var hasConflict = await transferAgreementRepository.HasDateOverlap(newProposal);

            if (hasConflict)
            {
                return ValidationProblem("There is already a Transfer Agreement with this company tin within the selected date range", statusCode: 409);
            }
        }

        await repository.AddTransferAgreementProposal(newProposal);

        return CreatedAtAction(nameof(GetTransferAgreementProposal), new { id = newProposal.Id }, newProposal);
    }

    /// <summary>
    /// Get TransferAgreementProposal by Id
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <response code="200">Successful operation</response>
    /// <response code="400">You cannot Accept/Deny your own TransferAgreementProposal, you cannot Accept/Deny a TransferAgreementProposal for another company or this proposal has run out</response>
    /// <response code="404">TransferAgreementProposal expired or deleted</response>
    [ProducesResponseType(typeof(TransferAgreementProposal), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult<TransferAgreementProposal>> GetTransferAgreementProposal(Guid id)
    {
        var proposal = await repository.GetNonExpiredTransferAgreementProposal(id);

        if (proposal == null)
        {
            return NotFound();
        }

        var currentCompanyId = Guid.Parse(User.FindSubjectGuidClaim());
        var currentCompanyTin = User.FindSubjectTinClaim();

        if (currentCompanyId == proposal.SenderCompanyId)
        {
            return ValidationProblem("You cannot Accept/Deny your own TransferAgreementProposal");
        }
        if (currentCompanyTin != proposal.ReceiverCompanyTin)
        {
            return ValidationProblem("You cannot Accept/Deny a TransferAgreementProposal for another company");
        }
        if (proposal.EndDate < DateTimeOffset.UtcNow)
        {
            return ValidationProblem("This proposal has run out");
        }

        return Ok(proposal);
    }

    /// <summary>
    /// Delete TransferAgreementProposal
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <response code="204">Successful operation</response>
    /// <response code="404">TransferAgreementProposal not found</response>
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransferAgreementProposal(Guid id)
    {
        var proposal = await repository.GetNonExpiredTransferAgreementProposal(id);

        if (proposal == null)
        {
            return NotFound();
        }

        await repository.DeleteTransferAgreementProposal(id);

        return NoContent();
    }
}
