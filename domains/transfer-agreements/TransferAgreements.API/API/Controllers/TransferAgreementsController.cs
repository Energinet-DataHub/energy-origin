using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.ApiModels.Requests;
using API.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    public class TransferAgreementsController : ControllerBase
    {
        private readonly ITransferAgreementService transferAgreementService;

        public TransferAgreementsController(ITransferAgreementService transferAgreementService)
        {
            this.transferAgreementService = transferAgreementService;
        }

        [HttpPost("api/transfer-agreements")]
        public async Task<ActionResult> Create([FromBody] CreateTransferAgreement request)
        {
            var transferAgreement = new TransferAgreement
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ReceiverTin = 12345678
            };

            var result = transferAgreementService.CreateTransferAgreement(transferAgreement);

            return Ok(result);
        }
    }
}
