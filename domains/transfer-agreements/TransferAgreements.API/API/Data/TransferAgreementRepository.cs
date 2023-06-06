using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class TransferAgreementRepository : ITransferAgreementRepository
{
    private readonly ApplicationDbContext context;

    public TransferAgreementRepository(ApplicationDbContext context) => this.context = context;

    public async Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement)
    {
        context.TransferAgreements.Add(transferAgreement);
        await context.SaveChangesAsync();
        return transferAgreement;
    }

    public async Task<List<TransferAgreementResponse>> GetTransferAgreementsBySubjectId(Guid subjectId)
    {
        return await context.TransferAgreements
            .Where(ta => ta.SenderId == subjectId)
            .Select(ta => new TransferAgreementResponse(
                ta.Id,
                ta.StartDate.ToUnixTimeSeconds(),
                ta.EndDate.ToUnixTimeSeconds(),
                ta.ReceiverTin))
            .ToListAsync();
    }

}
