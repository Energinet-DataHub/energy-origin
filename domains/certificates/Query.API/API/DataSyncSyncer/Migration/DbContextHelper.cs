using System.Collections.Generic;
using System.Threading.Tasks;
using API.ContractService;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.DataSyncSyncer.Migration;

public class DbContextHelper
{
    private readonly IDbContextFactory<ApplicationDbContext> factory;

    public DbContextHelper(IDbContextFactory<ApplicationDbContext> factory)
    {
        this.factory = factory;
    }

    public async Task<int> GetContractCount()
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        return await dbContext.Contracts.CountAsync();
    }

    public async Task<int> GetCertificatesCount()
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        return await dbContext.ProductionCertificates.CountAsync();
    }

    public async Task<int> GetSynchronizationCount()
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        return await dbContext.SynchronizationPositions.CountAsync();
    }

    public async Task SaveContracts(IEnumerable<CertificateIssuingContract> contracts)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        //Ensure timestamps are correct
        foreach (var contract in contracts)
        {
            contract.Created = contract.Created.ToUniversalTime();
            contract.StartDate = contract.StartDate.ToUniversalTime();
            if (contract.EndDate != null)
                contract.EndDate = contract.EndDate.Value.ToUniversalTime();

            dbContext.Contracts.Add(contract);
        }
        await dbContext.SaveChangesAsync();
    }
}
