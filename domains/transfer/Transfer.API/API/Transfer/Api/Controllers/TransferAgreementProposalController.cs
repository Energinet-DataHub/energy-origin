using System;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.Controllers;

[ApiController]
[Authorize(Policy.Frontend)]
[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
[Route("api/transfer/transfer-agreement-proposals")]
public class TransferAgreementProposalController(
    IValidator<CreateTransferAgreementProposal> createTransferAgreementProposalValidator,
    IUnitOfWork unitOfWork,
    IdentityDescriptor identityDescriptor,
    AccessDescriptor accessDescriptor)
    : ControllerBase
{
    /// <summary>
    /// Create TransferAgreementProposal
    /// </summary>
    /// <param name="organizationId">Sender organization id</param>
    /// <param name="request">The request object containing the StartDate, EndDate and ReceiverTin needed for creating the Transfer Agreement.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad request</response>
    /// <response code="409">There is already a Transfer Agreement with this company tin within the selected date range</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransferAgreementProposalResponse), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> CreateTransferAgreementProposal([FromQuery] Guid organizationId, CreateTransferAgreementProposal request)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var validateResult = await createTransferAgreementProposalValidator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var newProposal = new TransferAgreementProposal
        {
            SenderCompanyId = OrganizationId.Create(organizationId),
            SenderCompanyTin = Tin.Create(identityDescriptor.OrganizationCvr!),
            SenderCompanyName = OrganizationName.Create(identityDescriptor.OrganizationName),
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = String.IsNullOrWhiteSpace(request.ReceiverTin) ? null : Tin.Create(request.ReceiverTin),
            StartDate = UnixTimestamp.Create(request.StartDate),
            EndDate = request.EndDate == null ? null : UnixTimestamp.Create(request.EndDate.Value)
        };

        if (request.ReceiverTin != null)
        {
            if (request.ReceiverTin.Equals(identityDescriptor.OrganizationCvr))
            {
                return ValidationProblem(
                    "ReceiverTin cannot be the same as SenderTin.",
                    statusCode: 400);
            }

            var hasConflict = await unitOfWork.TransferAgreementRepo.HasDateOverlap(newProposal);
            if (hasConflict)
            {
                return ValidationProblem(
                    "There is already a Transfer Agreement with this company tin within the selected date range",
                    statusCode: 409);
            }
        }

        await unitOfWork.TransferAgreementProposalRepo.AddTransferAgreementProposal(newProposal);

        await AppendToActivityLog(identityDescriptor, newProposal, ActivityLogEntry.ActionTypeEnum.Created);

        await unitOfWork.SaveAsync();

        var response = new TransferAgreementProposalResponse(
            newProposal.Id,
            newProposal.SenderCompanyName.Value,
            newProposal.ReceiverCompanyTin?.Value,
            newProposal.StartDate.EpochSeconds,
            newProposal.EndDate?.EpochSeconds
        );
        return CreatedAtAction(nameof(GetTransferAgreementProposal), new { id = newProposal.Id }, response);
    }

    /// <summary>
    /// Get TransferAgreementProposal by Id
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <param name="organizationId"></param>
    /// <response code="200">Successful operation</response>
    /// <response code="400">You cannot Accept/Deny your own TransferAgreementProposal, you cannot Accept/Deny a TransferAgreementProposal for another company or this proposal has run out</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransferAgreementProposalResponse), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult<TransferAgreementProposalResponse>> GetTransferAgreementProposal([FromRoute] Guid id, [FromQuery] Guid organizationId)
    {
        var proposal = await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposal(id);

        if (proposal == null)
        {
            return NotFound();
        }

        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        if (organizationId == proposal.SenderCompanyId.Value)
        {
            return ValidationProblem("You cannot Accept/Deny your own TransferAgreementProposal");
        }

        if (proposal.ReceiverCompanyTin != null && identityDescriptor.OrganizationCvr != proposal.ReceiverCompanyTin.Value)
        {
            return ValidationProblem("You cannot Accept/Deny a TransferAgreementProposal for another company");
        }

        if (proposal.EndDate != null && proposal.EndDate < UnixTimestamp.Now())
        {
            return ValidationProblem("This proposal has run out");
        }

        return Ok(new TransferAgreementProposalResponse(
                proposal.Id,
                proposal.SenderCompanyName.Value,
                proposal.ReceiverCompanyTin?.Value,
                proposal.StartDate.EpochSeconds,
                proposal.EndDate?.EpochSeconds
            )
        );
    }

    /// <summary>
    /// Delete TransferAgreementProposal
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <param name="organizationId"></param>
    /// <response code="204">Successful operation</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> DeleteTransferAgreementProposal([FromRoute] Guid id, [FromQuery] Guid organizationId)
    {
        var proposal = await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposal(id);

        if (proposal == null)
        {
            return NotFound();
        }

        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        if (proposal.ReceiverCompanyTin != null && identityDescriptor.OrganizationCvr != proposal.ReceiverCompanyTin.Value)
        {
            return ValidationProblem("You cannot Deny a TransferAgreementProposal for another company");
        }

        await unitOfWork.TransferAgreementProposalRepo.DeleteTransferAgreementProposal(id);
        await AppendToActivityLog(identityDescriptor, proposal, ActivityLogEntry.ActionTypeEnum.Declined);

        await unitOfWork.SaveAsync();

        return NoContent();
    }

    private async Task AppendToActivityLog(IdentityDescriptor identity, TransferAgreementProposal proposal, ActivityLogEntry.ActionTypeEnum actionType)
    {
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
           actorId: identity.Subject,
           actorType: ActivityLogEntry.ActorTypeEnum.User,
           actorName: identity.Name,
           organizationTin: identity.OrganizationCvr!,
           organizationName: identity.OrganizationName,
           otherOrganizationTin: proposal.ReceiverCompanyTin?.Value ?? string.Empty,
           otherOrganizationName: string.Empty,
           entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
           actionType: actionType,
           entityId: proposal.Id.ToString())
        );
    }
}
