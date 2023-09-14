using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.Extensions;
using API.Models;
using API.Services;
using API.TransferAgreementsAutomation;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/transfer-agreements")]
public class TransferAgreementsController : ControllerBase
{
    private readonly ITransferAgreementRepository transferAgreementRepository;
    private readonly IValidator<CreateTransferAgreement> createTransferAgreementValidator;
    private readonly IProjectOriginWalletService projectOriginWalletService;
    private readonly MyCache cache;
    private readonly IHttpContextAccessor httpContextAccessor;

    public TransferAgreementsController(
        ITransferAgreementRepository transferAgreementRepository,
        IValidator<CreateTransferAgreement> createTransferAgreementValidator,
        IProjectOriginWalletService projectOriginWalletService,
        MyCache cache,
        IHttpContextAccessor httpContextAccessor)
    {
        this.transferAgreementRepository = transferAgreementRepository;
        this.createTransferAgreementValidator = createTransferAgreementValidator;
        this.projectOriginWalletService = projectOriginWalletService;
        this.cache = cache;
        this.httpContextAccessor = httpContextAccessor;
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
        catch (DbUpdateException e)
        {
            return Conflict();
        }

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

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        return Ok(cache.Cache.Get(CacheValues.Key));
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
