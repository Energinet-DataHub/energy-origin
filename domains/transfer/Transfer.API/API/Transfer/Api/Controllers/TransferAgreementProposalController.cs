using System;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/transfer/transfer-agreement-proposals")]
public class TransferAgreementProposalController(
    IValidator<CreateTransferAgreementProposal> createTransferAgreementProposalValidator,
    IUnitOfWork unitOfWork)
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
        var user = new UserDescriptor(User);

        var validateResult = await createTransferAgreementProposalValidator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var newProposal = new TransferAgreementProposal
        {
            SenderCompanyId = user.Subject,
            SenderCompanyTin = user.Organization!.Tin,
            SenderCompanyName = user.Organization.Name,
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = request.ReceiverTin,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = request.EndDate == null ? null : DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value)
        };

        if (request.ReceiverTin != null)
        {
            var hasConflict = await unitOfWork.TransferAgreementRepo.HasDateOverlap(newProposal);

            if (hasConflict)
            {
                return ValidationProblem(
                    "There is already a Transfer Agreement with this company tin within the selected date range",
                    statusCode: 409);
            }
        }

        await unitOfWork.TransferAgreementProposalRepo.AddTransferAgreementProposal(newProposal);

        await AppendToActivityLog(user, newProposal, ActivityLogEntry.ActionTypeEnum.Created);

        await unitOfWork.SaveAsync();

        var response = new TransferAgreementProposalResponse(
            newProposal.Id,
            newProposal.SenderCompanyName,
            newProposal.ReceiverCompanyTin,
            newProposal.StartDate.ToUnixTimeSeconds(),
            newProposal.EndDate?.ToUnixTimeSeconds()
        );
        return CreatedAtAction(nameof(GetTransferAgreementProposal), new { id = newProposal.Id }, response);
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
        var proposal = await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposal(id);

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
        var proposal = await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposal(id);

        if (proposal == null)
        {
            return NotFound();
        }

        var user = new UserDescriptor(User);

        if (proposal.ReceiverCompanyTin != null && user.Organization!.Tin != proposal.ReceiverCompanyTin)
        {
            return ValidationProblem("You cannot Deny a TransferAgreementProposal for another company");
        }

        await unitOfWork.TransferAgreementProposalRepo.DeleteTransferAgreementProposal(id);
        await AppendToActivityLog(user, proposal, ActivityLogEntry.ActionTypeEnum.Declined);

        await unitOfWork.SaveAsync();

        return NoContent();
    }

    private async Task AppendToActivityLog(UserDescriptor user, TransferAgreementProposal proposal, ActivityLogEntry.ActionTypeEnum actionType)
    {
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
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
