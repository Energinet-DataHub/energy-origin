using System;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Data;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext context = context ?? throw new ArgumentNullException(nameof(context));
    private IDbContextTransaction? transaction;

    public async Task BeginTransactionAsync()
    {
        transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (transaction is null)
            throw new InvalidOperationException("No transaction started.");

        try
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            transaction = null;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
            transaction = null;
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (transaction is not null)
        {
            await RollbackAsync();
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeTransactionAsync();
        await context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
