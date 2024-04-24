using System;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Data;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private bool _disposed;
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction started.");

        try
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_disposed) return;
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        await _context.DisposeAsync();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
