using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.ApiModels;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;

namespace API.Controllers;

[ApiController]
    public class TransferAgreementsController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public TransferAgreementsController(ApplicationDbContext context) => this.context = context;

        [HttpGet("api/transfer-agreements")]
        public async Task<ActionResult<IEnumerable<TransferAgreementResponse>>> Index([FromQuery] SubjectIdRequest request)
        {
            var subjectId = request.SubjectId;

            var transferAgreements = await context.TransferAgreements
                .Include(ta => ta.Sender)
                .Include(ta => ta.Receiver)
                .Where(ta => ta.Sender.Id == subjectId || ta.Receiver.Id == subjectId)
                .ToListAsync();

            var result = transferAgreements.Select(ta => new TransferAgreementResponse
            {
                Id = ta.Id,
                StartDate = ta.StartDate,
                EndDate = ta.EndDate,
                Sender = new SubjectResponse
                {
                    Id = ta.Sender.Id,
                    Name = ta.Sender.Name,
                    Tin = ta.Sender.Tin
                },
                Receiver = new SubjectResponse
                {
                    Id = ta.Receiver.Id,
                    Name = ta.Receiver.Name,
                    Tin = ta.Receiver.Tin
                }
            });

            return Ok(result);
        }

        [HttpPost("api/transfer-agreements")]
        public async Task<ActionResult> Create([FromBody] TransferAgreementCreateRequest request)
        {
            var sender = await context.Subjects.FindAsync(request.SenderId);
            if (sender == null) return BadRequest($"Invalid sender ID: {request.SenderId}");

            var receiver = await context.Subjects.FindAsync(request.ReceiverId);
            if (receiver == null) return BadRequest($"Invalid receiver ID: {request.ReceiverId}");

            var transferAgreement = new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Sender = sender,
                Receiver = receiver
            };
            context.TransferAgreements.Add(transferAgreement);
            await context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("api/transfer-agreements/{id}")]
        public async Task<ActionResult<TransferAgreementResponse>> Show(Guid id)
        {
            var transferAgreement = await context.TransferAgreements
                .Include(ta => ta.Sender)
                .Include(ta => ta.Receiver)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (transferAgreement == null) return NotFound();

            var response = new TransferAgreementResponse
            {
                Id = transferAgreement.Id,
                StartDate = transferAgreement.StartDate,
                EndDate = transferAgreement.EndDate,
                Sender = new SubjectResponse
                {
                    Id = transferAgreement.Sender.Id,
                    Name = transferAgreement.Sender.Name,
                    Tin = transferAgreement.Sender.Tin
                },
                Receiver = new SubjectResponse
                {
                    Id = transferAgreement.Receiver.Id,
                    Name = transferAgreement.Receiver.Name,
                    Tin = transferAgreement.Receiver.Tin
                }
            };

            return Ok(response);
        }
    }
