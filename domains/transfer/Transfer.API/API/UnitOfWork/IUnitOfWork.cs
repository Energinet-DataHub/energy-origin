using System;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using DataContext;
using EnergyOrigin.ActivityLog.API;

namespace API.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    ITransferAgreementRepository TransferAgreementRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }
    ITransferAgreementProposalRepository TransferAgreementProposalRepo { get; }
    ITransferAgreementHistoryEntryRepository TransferAgreementHistoryEntryRepo { get; }

    Task SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private TransferDbContext context;
    private ITransferAgreementRepository transferAgreementRepo = null!;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ITransferAgreementProposalRepository transferAgreementProposalRepo = null!;
    private ITransferAgreementHistoryEntryRepository transferAgreementHistoryEntryRepo = null!;

    public UnitOfWork(TransferDbContext context)
    {
        this.context = context;
    }

    public ITransferAgreementRepository TransferAgreementRepo
    {
        get => transferAgreementRepo ??= new TransferAgreementRepository(context);
    }

    public IActivityLogEntryRepository ActivityLogEntryRepo
    {
        get => activityLogEntryRepo ??= new ActivityLogEntryRepository(context);
    }

    public ITransferAgreementProposalRepository TransferAgreementProposalRepo
    {
        get => transferAgreementProposalRepo ??= new TransferAgreementProposalRepository(context);
    }

    public ITransferAgreementHistoryEntryRepository TransferAgreementHistoryEntryRepo
    {
        get => transferAgreementHistoryEntryRepo ??= new TransferAgreementHistoryEntryRepository(context);
    }

    public Task SaveAsync()
    {
        return context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}
