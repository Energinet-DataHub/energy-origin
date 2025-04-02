using System;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Data;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private IDbContextTransaction? _transaction;
    private bool _ownsTransaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is not null)
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

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction started.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_ownsTransaction)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
            _transaction = null;
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_ownsTransaction && _transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    private async Task DisposeTransactionAsync(CancellationToken cancellationToken)
    {
        if (_ownsTransaction && _transaction is not null)
        {
            await RollbackAsync(cancellationToken);
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_ownsTransaction)
        {
            await DisposeTransactionAsync(CancellationToken.None);
            await _context.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
