using System.Threading.Tasks;
using API.Data;
using API.ApiModels.Requests;
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

            var result = await transferAgreementService.CreateTransferAgreement(transferAgreement);

            return Ok(result);
        }
    }
}
