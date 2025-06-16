using System;
using System.Threading.Tasks;
using API.ClaimAutomation.Api.Repositories;
using API.Transfer.Api.Repository;
using DataContext;
using EnergyOrigin.ActivityLog.API;

namespace API.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    ITransferAgreementRepository TransferAgreementRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }
    ITransferAgreementProposalRepository TransferAgreementProposalRepo { get; }
    IClaimAutomationRepository ClaimAutomationRepository { get; }
    IReportRepository ReportRepository { get; }

    Task SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext context;
    private ITransferAgreementRepository transferAgreementRepo = null!;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ITransferAgreementProposalRepository transferAgreementProposalRepo = null!;
    private IClaimAutomationRepository claimAutomationRepository = null!;
    private IReportRepository reportRepository = null!;

    public UnitOfWork(ApplicationDbContext context)
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

    public IClaimAutomationRepository ClaimAutomationRepository
    {
        get => claimAutomationRepository ??= new ClaimAutomationRepository(context);
    }

    public IReportRepository ReportRepository
    {
        get => reportRepository ??= new ReportRepository(context);
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
