using System.Threading.Tasks;

namespace API.Data;

public class TransferAgreementService : ITransferAgreementService
{
    private readonly ApplicationDbContext context;

    public TransferAgreementService(ApplicationDbContext context) => this.context = context;

    public async Task<TransferAgreement> CreateTransferAgreement(TransferAgreement transferAgreement)
    {
        context.TransferAgreements.Add(transferAgreement);
        await context.SaveChangesAsync();
        return transferAgreement;
    }
}
