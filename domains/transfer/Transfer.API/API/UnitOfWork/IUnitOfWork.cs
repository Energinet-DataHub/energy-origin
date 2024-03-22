using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using DataContext;
using EnergyOrigin.ActivityLog.API;

namespace API.UnitOfWork;

public interface IUnitOfWork
{
    ITransferAgreementRepository TransferAgreementRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }
    ITransferAgreementProposalRepository TransferAgreementProposalRepo { get; }

    Task SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext context;
    private ITransferAgreementRepository transferAgreementRepo = null!;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ITransferAgreementProposalRepository transferAgreementProposalRepo = null!;

    public UnitOfWork(ApplicationDbContext context)
    {
        this.context = context;
    }

    public ITransferAgreementRepository TransferAgreementRepo
    {
        get
        {
            return transferAgreementRepo ??= new TransferAgreementRepository(context);
        }
    }

    public IActivityLogEntryRepository ActivityLogEntryRepo
    {
        get
        {
            return activityLogEntryRepo ??= new ActivityLogEntryRepository(context);
        }
    }

    public ITransferAgreementProposalRepository TransferAgreementProposalRepo
    {
        get
        {
            return transferAgreementProposalRepo ??= new TransferAgreementProposalRepository(context);
        }
    }

    public Task SaveAsync()
    {
        return context.SaveChangesAsync();
    }
}
