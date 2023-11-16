using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Shared.Extensions;
using API.Shared.Helpers;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.Transfer.Api.Models;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/transfer-agreements")]
public class TransferAgreementsController : ControllerBase
{
    private readonly ITransferAgreementRepository transferAgreementRepository;
    private readonly IValidator<CreateTransferAgreement> createTransferAgreementValidator;
    private readonly IProjectOriginWalletService projectOriginWalletService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ITransferAgreementProposalRepository transferAgreementProposalRepository;

    public TransferAgreementsController(
        ITransferAgreementRepository transferAgreementRepository,
        IValidator<CreateTransferAgreement> createTransferAgreementValidator,
        IProjectOriginWalletService projectOriginWalletService,
        IHttpContextAccessor httpContextAccessor,
        ITransferAgreementProposalRepository transferAgreementProposalRepository)
    {
        this.transferAgreementRepository = transferAgreementRepository;
        this.createTransferAgreementValidator = createTransferAgreementValidator;
        this.projectOriginWalletService = projectOriginWalletService;
        this.httpContextAccessor = httpContextAccessor;
        this.transferAgreementProposalRepository = transferAgreementProposalRepository;
    }

    [ProducesResponseType(201)]
    [ProducesResponseType(409)]
    [ProducesResponseType(400)]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateTransferAgreement request)
    {
        var subject = User.FindSubjectGuidClaim();
        var subjectName = User.FindSubjectNameClaim();
        var subjectTin = User.FindSubjectTinClaim();

        var validateResult = await createTransferAgreementValidator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var transferAgreement = new TransferAgreement
        {
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = request.EndDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value) : null,
            SenderId = Guid.Parse(subject),
            SenderName = subjectName,
            SenderTin = subjectTin,
            ReceiverTin = request.ReceiverTin
        };

        if (await transferAgreementRepository.HasDateOverlap(transferAgreement))
        {
            return Conflict();
        }

        var bearerToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"]).ToString();

        transferAgreement.ReceiverReference = await projectOriginWalletService.CreateReceiverDepositEndpoint(
            bearerToken,
            request.Base64EncodedWalletDepositEndpoint,
            request.ReceiverTin);

        try
        {
            var result = await transferAgreementRepository.AddTransferAgreementToDb(transferAgreement);

            return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }
    }

    /// <summary>
    /// Add a new Transfer Agreement
    /// </summary>
    /// <param name="request">The request object containing the TransferAgreementProposalId for creating the Transfer Agreement.</param>
    /// <response code="201">Successful operation</response>
    /// <response code="400">Only the receiver company can accept this Transfer Agreement Proposal or the proposal has run out</response>
    /// <response code="404">TransferAgreementProposal expired or deleted</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransferAgreement), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> CreateFromTransferAgreementProposal(CreateTransferAgreementFromProposal request)
    {
        var proposal = await transferAgreementProposalRepository.GetNonExpiredTransferAgreementProposal(request.TransferAgreementProposalId);
        if (proposal == null)
        {
            return NotFound("TransferAgreementProposal expired or deleted");
        }

        var subjectTin = User.FindSubjectTinClaim();
        if (proposal.ReceiverCompanyTin != subjectTin)
        {
            return BadRequest("Only the receiver company can accept this Transfer Agreement Proposal");
        }

        if (proposal.EndDate < DateTimeOffset.UtcNow)
        {
            return BadRequest("This proposal has run out");
        }

        var receiverBearerToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"]).ToString();
        var receiverWdeBase64String = await projectOriginWalletService.CreateWalletDepositEndpoint(receiverBearerToken);

        var senderBearerToken = ProjectOriginWalletHelper.GenerateBearerToken(proposal.SenderCompanyId.ToString());

        var receiverReference = await projectOriginWalletService.CreateReceiverDepositEndpoint(
            senderBearerToken,
            receiverWdeBase64String,
            proposal.ReceiverCompanyTin);

        var transferAgreement = new TransferAgreement
        {
            StartDate = proposal.StartDate,
            EndDate = proposal.EndDate,
            SenderId = proposal.SenderCompanyId,
            SenderName = proposal.SenderCompanyName,
            SenderTin = proposal.SenderCompanyTin,
            ReceiverTin = proposal.ReceiverCompanyTin,
            ReceiverReference = receiverReference
        };

        var result = await transferAgreementRepository.AddTransferAgreementToDb(transferAgreement);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
    }

    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult> Get([FromRoute] Guid id)
    {
        var tin = User.FindSubjectTinClaim();
        var subject = User.FindSubjectGuidClaim();

        var result = await transferAgreementRepository.GetTransferAgreement(id, subject, tin);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(ToTransferAgreementDto(result));
    }

    [ProducesResponseType(typeof(TransferAgreementsResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements()
    {
        var subject = User.FindSubjectGuidClaim();
        var userTin = User.FindSubjectTinClaim();

        var transferAgreements = await transferAgreementRepository.GetTransferAgreementsList(Guid.Parse(subject), userTin);

        if (!transferAgreements.Any())
        {
            return NoContent();
        }

        var listResponse = transferAgreements.Select(ToTransferAgreementDto)
            .ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(409)]
    [HttpPatch("{id}")]
    public async Task<ActionResult<EditTransferAgreementEndDate>> EditEndDate(Guid id, [FromBody] EditTransferAgreementEndDate request)
    {

        var subject = User.FindSubjectGuidClaim();
        var userTin = User.FindSubjectTinClaim();

        var validator = new EditTransferAgreementEndDateValidator();

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var endDate = request.EndDate.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value)
            : (DateTimeOffset?)null;
        var senderId = Guid.Parse(User.FindSubjectGuidClaim());
        var transferAgreement = await transferAgreementRepository.GetTransferAgreement(id, subject, userTin);

        if (transferAgreement == null || transferAgreement.SenderId != senderId)
        {
            return NotFound();
        }

        if (transferAgreement.EndDate < DateTimeOffset.UtcNow)
            return ValidationProblem("Transfer agreement has expired", statusCode: 400);

        if (await transferAgreementRepository.HasDateOverlap(new TransferAgreement
        {
            Id = transferAgreement.Id,
            StartDate = transferAgreement.StartDate,
            EndDate = endDate,
            SenderId = transferAgreement.SenderId,
            ReceiverTin = transferAgreement.ReceiverTin
        }))
        {
            return Conflict("Transfer agreement date overlap");
        }

        transferAgreement.EndDate = endDate;

        await transferAgreementRepository.Save();

        var response = new TransferAgreementDto(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate?.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin);

        return Ok(response);
    }

    [ProducesResponseType(typeof(string), 200)]
    [HttpPost("wallet-deposit-endpoint")]
    public async Task<ActionResult> CreateWalletDepositEndpoint()
    {
        var bearerToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"]).ToString();

        var base64String = await projectOriginWalletService.CreateWalletDepositEndpoint(bearerToken);
        return Ok(new { result = base64String });
    }

    private static TransferAgreementDto ToTransferAgreementDto(TransferAgreement transferAgreement) =>
        new(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate?.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin
        );
}
