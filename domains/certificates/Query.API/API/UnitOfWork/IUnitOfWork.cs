using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Repositories;
using API.MeasurementsSyncer.Persistence;
using DataContext;
using EnergyOrigin.ActivityLog.API;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    ICertificateIssuingContractRepository CertificateIssuingContractRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }
    ISlidingWindowState SlidingWindowState { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _ownsTransaction;
    private IActivityLogEntryRepository? _activityLogEntryRepo;
    private ICertificateIssuingContractRepository? _certificateIssuingContractRepo;
    private ISlidingWindowState? _slidingWindowState;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public ICertificateIssuingContractRepository CertificateIssuingContractRepo =>
        _certificateIssuingContractRepo ??= new CertificateIssuingContractRepository(_context);

    public IActivityLogEntryRepository ActivityLogEntryRepo =>
        _activityLogEntryRepo ??= new ActivityLogEntryRepository(_context);

    public ISlidingWindowState SlidingWindowState =>
        _slidingWindowState ??= new SlidingWindowState(_context);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null) return;

        if (_context.Database.CurrentTransaction != null)
        {
            _transaction = _context.Database.CurrentTransaction;
            _ownsTransaction = false;
        }
        else
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _ownsTransaction = true;
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) throw new InvalidOperationException("Transaction not started");

        await _context.SaveChangesAsync(cancellationToken);

        if (_ownsTransaction)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        if (_ownsTransaction)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await RollbackAsync();
        }

        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
