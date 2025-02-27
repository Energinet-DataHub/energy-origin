using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.Controllers;

[ApiController]
[Authorize(Policy.Frontend)]
[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
[Route("api/transfer/transfer-agreements")]
public class TransferAgreementsController(
    IMediator mediator,
    IUnitOfWork unitOfWork,
    IdentityDescriptor identityDescriptor,
    AccessDescriptor accessDescriptor
) : ControllerBase
{
    /// <summary>
    /// Add a new Transfer Agreement
    /// </summary>
    /// <param name="request">The request object containing the TransferAgreementProposalId for creating the Transfer Agreement.</param>
    /// <param name="organizationId"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="201">Successful operation</response>
    /// <response code="400">Only the receiver company can accept this Transfer Agreement Proposal or the proposal has run out</response>
    /// <response code="409">There is already a Transfer Agreement with proposals company tin within the selected date range</response>
    [HttpPost()]
    [ProducesResponseType(typeof(TransferAgreement), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> Create(CreateTransferAgreement request, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var command = new AcceptTransferAgreementProposalCommand(request.TransferAgreementProposalId, organizationId);
        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.TransferAgreementId },
            ToTransferAgreementDto(result.TransferAgreementId, result.SenderTin, result.SenderName, result.ReceiverTin,
                result.StartDate,
                result.EndDate, result.Type));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> Get([FromRoute] Guid id, [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var result = await unitOfWork.TransferAgreementRepo.GetTransferAgreement(id, organizationId.ToString(),
            identityDescriptor.OrganizationCvr!,
            cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(TransferAgreementDtoMapper.MapTransferAgreement(result));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 409)]
    public async Task<ActionResult<EditTransferAgreementEndDate>> EditEndDate([FromRoute] Guid id,
        [FromBody] EditTransferAgreementEndDate request, CancellationToken cancellationToken)
    {
        var validator = new EditTransferAgreementEndDateValidator();

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var command = new EditTransferAgreementEndDateCommand(id, request.EndDate);
        var result = await mediator.Send(command, cancellationToken);

        var response = TransferAgreementDtoMapper.MapTransferAgreement(result.TransferAgreement);
        return Ok(response);
    }

    private static TransferAgreementDto ToTransferAgreementDto(Guid transferAgreementId, string senderTin,
        string senderName, string receiverTin,
        long startDate, long? endDate, TransferAgreementType type)
    {
        return new(
            Id: transferAgreementId,
            StartDate: startDate,
            EndDate: endDate,
            SenderName: senderName,
            SenderTin: senderTin,
            ReceiverTin: receiverTin,
            Type: TransferAgreementTypeMapper.MapCreateTransferAgreementType(type)
        );
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(TransferAgreementOverviewResponse), 200)]
    public async Task<ActionResult<TransferAgreementOverviewResponse>> GetTransferAgreementsOverview(
        CancellationToken cancellationToken)
    {
        var queryResult =
            await mediator.Send(new GetTransferAgreementsQuery(identityDescriptor.OrganizationId,
                identityDescriptor.OrganizationCvr!), cancellationToken);

        var dto = queryResult
            .Result
            .Select(x =>
                new TransferAgreementOverviewDto(x.Id, x.StartDate, x.EndDate, x.SenderName, x.SenderTin, x.ReceiverTin, x.Type, x.TransferAgreementStatus))
            .ToList();

        return Ok(new TransferAgreementOverviewResponse(dto));
    }

    [HttpGet("overview/consent")]
    [ProducesResponseType(typeof(TransferAgreementOverviewResponse), 200)]
    public async Task<ActionResult<TransferAgreementOverviewResponse>> GetConsentTransferAgreementsOverview(
        CancellationToken cancellationToken)
    {
        var orgIds = identityDescriptor.AuthorizedOrganizationIds;

        var queryResult = await mediator.Send(new GetConsentTransferAgreementsQuery(orgIds), cancellationToken);

        var dto = queryResult
            .Result
            .Select(x =>
                new TransferAgreementOverviewDto(x.Id, x.StartDate, x.EndDate, x.SenderName, x.SenderTin, x.ReceiverTin, x.Type, x.TransferAgreementStatus))
            .ToList();

        return Ok(new TransferAgreementOverviewResponse(dto));
    }

    [HttpPost("create/")]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> CreateTransferAgreementDirectly(CreateTransferAgreementRequest request)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganizations([request.SenderOrganizationId, request.ReceiverOrganizationId]);

        var validator = new CreateTransferAgreementValidator();

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var command = await mediator.Send(new CreateTransferAgreementCommand(request.ReceiverOrganizationId, request.SenderOrganizationId,
            request.StartDate, request.EndDate, CreateTransferAgreementTypeMapper.MapCreateTransferAgreementType(request.Type)), CancellationToken.None);

        return CreatedAtAction(nameof(Get), new { id = command.TransferAgreementId }, ToTransferAgreementDto(
            command.TransferAgreementId,
            command.SenderTin, command.SenderName, command.ReceiverTin, command.StartDate, command.EndDate,
            command.Type));
    }
}
