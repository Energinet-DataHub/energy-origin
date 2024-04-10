using System;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;

namespace API.ContractService.Repositories;

public interface IWalletRepository
{
    Task AddWallet(Wallet wallet);
    Task<Wallet?> GetWalletByOwnerSubject(Guid ownerSubject, CancellationToken cancellationToken);
}

public class WalletRepository : IWalletRepository
{
    private readonly ApplicationDbContext dbContext;

    public WalletRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddWallet(Wallet wallet)
    {
        await dbContext.Wallets.AddAsync(wallet);
    }

    public async Task<Wallet?> GetWalletByOwnerSubject(Guid ownerSubject, CancellationToken cancellationToken)
    {
        return await dbContext.Wallets.FirstOrDefaultAsync(x => x.OwnerSubject == ownerSubject, cancellationToken);
    }
}
