using System;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin)
    {
        return await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id && (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
            .FirstOrDefaultAsync();
    }
}
