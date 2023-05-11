using System;
using System.Threading.Tasks;
using API.Api.ApiModels.Requests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Api.Create;

public class CreateTransferAgreementApi
{
    private readonly CreateTransferAgreementService createTransferAgreementService;

    public CreateTransferAgreementApi(CreateTransferAgreementService createTransferAgreementService)
    {
        this.createTransferAgreementService = createTransferAgreementService;
    }

    [HttpPost("api/transfer-agreements")]
    public async Task<ActionResult> Create([FromBody] TransferAgreementCreateRequest request)
    {
        var transferAgreement = await createTransferAgreementService.Create(request.StartDate, request.EndDate, request.SenderId, request.ReceiverId);

        return Ok(transferAgreement);
    }

}
