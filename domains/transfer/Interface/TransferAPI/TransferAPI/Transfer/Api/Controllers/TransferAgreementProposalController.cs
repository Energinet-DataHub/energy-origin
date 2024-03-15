using System;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using Asp.Versioning;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Transfer.Application.Commands;
using Transfer.Application.Repositories;
using Transfer.Domain.Entities;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/transfer/transfer-agreement-proposals")]
public class TransferAgreementProposalController(
    ITransferAgreementProposalRepository repository,
    IValidator<CreateTransferAgreementProposal> createTransferAgreementProposalValidator,
    IActivityLogEntryRepository activityLogEntryRepository,
    IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// Create TransferAgreementProposal
    /// </summary>
    /// <param name="request">The request object containing the StartDate, EndDate and ReceiverTin needed for creating the Transfer Agreement.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad request</response>
    /// <response code="409">There is already a Transfer Agreement with this company tin within the selected date range</response>
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementProposalResponse), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 409)]
    [HttpPost]
    public async Task<ActionResult> CreateTransferAgreementProposal(CreateTransferAgreementProposal request)
    {
        var validateResult = await createTransferAgreementProposalValidator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var cmd = new CreateTransferAgreementProposalCommand(request.StartDate, request.EndDate, request.ReceiverTin);
        var result = await mediator.Send(cmd);

        var response = new TransferAgreementProposalResponse(
            result.Id,
            result.SenderCompanyName,
            result.ReceiverCompanyTin,
            result.StartDate,
            result.EndDate
        );
        return CreatedAtAction(nameof(GetTransferAgreementProposal), new { id = response.Id }, response);
    }

    /// <summary>
    /// Get TransferAgreementProposal by Id
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <response code="200">Successful operation</response>
    /// <response code="400">You cannot Accept/Deny your own TransferAgreementProposal, you cannot Accept/Deny a TransferAgreementProposal for another company or this proposal has run out</response>
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementProposalResponse), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult<TransferAgreementProposalResponse>> GetTransferAgreementProposal(Guid id)
    {
        var proposal = await repository.GetNonExpiredTransferAgreementProposal(id);

        if (proposal == null)
        {
            return NotFound();
        }

        var user = new UserDescriptor(User);

        if (user.Subject == proposal.SenderCompanyId)
        {
            return ValidationProblem("You cannot Accept/Deny your own TransferAgreementProposal");
        }

        if (proposal.ReceiverCompanyTin != null && user.Organization!.Tin != proposal.ReceiverCompanyTin)
        {
            return ValidationProblem("You cannot Accept/Deny a TransferAgreementProposal for another company");
        }

        if (proposal.EndDate < DateTimeOffset.UtcNow)
        {
            return ValidationProblem("This proposal has run out");
        }

        return Ok(new TransferAgreementProposalResponse(
                proposal.Id,
                proposal.SenderCompanyName,
                proposal.ReceiverCompanyTin,
                proposal.StartDate.ToUnixTimeSeconds(),
                proposal.EndDate?.ToUnixTimeSeconds()
            )
        );
    }

    /// <summary>
    /// Delete TransferAgreementProposal
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <response code="204">Successful operation</response>
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

        var user = new UserDescriptor(User);

        if (proposal.ReceiverCompanyTin != null && user.Organization!.Tin != proposal.ReceiverCompanyTin)
        {
            return ValidationProblem("You cannot Deny a TransferAgreementProposal for another company");
        }

        await repository.DeleteTransferAgreementProposal(id);
        await AppendToActivityLog(user, proposal, ActivityLogEntry.ActionTypeEnum.Declined);

        return NoContent();
    }

    private async Task AppendToActivityLog(UserDescriptor user, TransferAgreementProposal proposal, ActivityLogEntry.ActionTypeEnum actionType)
    {
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(
           actorId: user.Subject,
           actorType: ActivityLogEntry.ActorTypeEnum.User,
           actorName: user.Name,
           organizationTin: user.Organization!.Tin,
           organizationName: user.Organization!.Name,
           otherOrganizationTin: proposal.ReceiverCompanyTin ?? string.Empty,
           otherOrganizationName: string.Empty,
           entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
           actionType: actionType,
           entityId: proposal.Id.ToString())
        );
    }
}
