using System;
using System.Threading.Tasks;
using API.ClaimAutomation.Api.Repositories;
using API.Transfer.Api.Repository;
using DataContext;
using EnergyOrigin.ActivityLog.API;
using Microsoft.EntityFrameworkCore;

namespace API.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    ITransferAgreementRepository TransferAgreementRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }
    ITransferAgreementProposalRepository TransferAgreementProposalRepo { get; }
    IClaimAutomationRepository ClaimAutomationRepository { get; }

    Task SaveAsync();
}
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private ApplicationDbContext? _context;

    private ITransferAgreementRepository transferAgreementRepo = null!;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ITransferAgreementProposalRepository transferAgreementProposalRepo = null!;
    private IClaimAutomationRepository claimAutomationRepository = null!;

    public UnitOfWork(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private ApplicationDbContext Context => _context ??= _dbContextFactory.CreateDbContext();

    public ITransferAgreementRepository TransferAgreementRepo =>
        transferAgreementRepo ??= new TransferAgreementRepository(Context);

    public IActivityLogEntryRepository ActivityLogEntryRepo =>
        activityLogEntryRepo ??= new ActivityLogEntryRepository(Context);

    public ITransferAgreementProposalRepository TransferAgreementProposalRepo =>
        transferAgreementProposalRepo ??= new TransferAgreementProposalRepository(Context);

    public IClaimAutomationRepository ClaimAutomationRepository =>
        claimAutomationRepository ??= new ClaimAutomationRepository(Context);

    public Task SaveAsync() => Context.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
            await _context.DisposeAsync();
    }
}
