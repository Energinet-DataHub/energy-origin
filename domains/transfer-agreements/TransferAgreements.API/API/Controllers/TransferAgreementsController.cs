using System;
using System.Threading.Tasks;
using API.ApiModels;
using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    public class TransferAgreementController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public TransferAgreementController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpPost("api/transfer-agreements")]
        public async Task<ActionResult<TransferAgreement>> Create([FromBody] TransferAgreement transferAgreement)
        {
            // Validate the input model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Ensure the sender and receiver subjects exist
            var sender = await context.Subjects.FindAsync(transferAgreement.SenderTin, transferAgreement.SenderName);
            var receiver = await context.Subjects.FindAsync(transferAgreement.ReceiverTin, transferAgreement.ReceiverName);

            if (sender == null || receiver == null)
            {
                return BadRequest("Sender or receiver not found.");
            }

            transferAgreement.Sender = sender;
            transferAgreement.Receiver = receiver;

            // Add the transfer agreement to the database
            context.TransferAgreements.Add(transferAgreement);
            await context.SaveChangesAsync();

            // Return the created transfer agreement
            return CreatedAtAction(nameof(GetById), new { id = transferAgreement.Id }, transferAgreement);
        }

        // Add a method to retrieve a transfer agreement by ID
        [HttpGet("api/transfer-agreement/{id}")]
        public async Task<ActionResult<TransferAgreement>> GetById(Guid id)
        {
            var transferAgreement = await context.TransferAgreements
                .Include(ta => ta.Sender)
                .Include(ta => ta.Receiver)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (transferAgreement == null)
            {
                return NotFound();
            }

            return transferAgreement;
        }
    }

    }
