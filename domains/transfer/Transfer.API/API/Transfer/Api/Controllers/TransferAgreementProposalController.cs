using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
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
    IValidator<CreateTransferAgreementProposalRequest> createTransferAgreementProposalRequestValidator,
    IUnitOfWork unitOfWork,
    IdentityDescriptor identityDescriptor,
    AccessDescriptor accessDescriptor,
    IMediator mediator)
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
    [ProducesResponseType(typeof(void), 403)]
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

        var command = new CreateTransferAgreementProposalCommand(organizationId, request.ReceiverTin, request.StartDate, request.EndDate, CreateTransferAgreementTypeMapper.MapCreateTransferAgreementType(request.Type));
        var result = await mediator.Send(command);

        var response = new TransferAgreementProposalResponse(
            result.Id,
            result.SenderOrganizationName,
            result.ReceiverOrganizationTin,
            result.StartDate,
            result.EndDate,
            TransferAgreementTypeMapper.MapCreateTransferAgreementType(result.Type)
        );

        return CreatedAtAction(nameof(GetTransferAgreementProposal), new { id = result.Id }, response);
    }

    /// <summary>
    /// Get TransferAgreementProposal by Id
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <param name="organizationId"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Successful operation</response>
    /// <response code="400">You cannot Accept/Deny your own TransferAgreementProposal, you cannot Accept/Deny a TransferAgreementProposal for another company or this proposal has run out</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransferAgreementProposalResponse), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult<TransferAgreementProposalResponse>> GetTransferAgreementProposal([FromRoute] Guid id,
        [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var proposal = await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposal(id, cancellationToken);

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
                proposal.EndDate?.EpochSeconds,
                TransferAgreementTypeMapper.MapCreateTransferAgreementType(proposal.Type)
            )
        );
    }

    /// <summary>
    /// Delete TransferAgreementProposal
    /// </summary>
    /// <param name="id">Id of TransferAgreementProposal</param>
    /// <param name="cancellationToken"></param>
    /// <response code="204">Successful operation</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> DeleteTransferAgreementProposal([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteTransferAgreementProposalCommand(id);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("create/")]
    [ProducesResponseType(typeof(TransferAgreementProposalResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> CreateTransferAgreementDirectly(CreateTransferAgreementProposalRequest request)
    {
        var organizationId = request.SenderOrganizationId;
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var validateResult = await createTransferAgreementProposalRequestValidator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var command = new CreateTransferAgreementProposalCommand(organizationId, request.ReceiverTin, request.StartDate, request.EndDate, CreateTransferAgreementTypeMapper.MapCreateTransferAgreementType(request.Type));
        var result = await mediator.Send(command);

        var response = new TransferAgreementProposalResponse(
            result.Id,
            result.SenderOrganizationName,
            result.ReceiverOrganizationTin,
            result.StartDate,
            result.EndDate,
            TransferAgreementTypeMapper.MapCreateTransferAgreementType(result.Type)
        );

        return CreatedAtAction(nameof(GetTransferAgreementProposal), new { id = result.Id }, response);
    }
}
