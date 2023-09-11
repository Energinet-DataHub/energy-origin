using API.ContractService;
using API.Data;
using CertificateEvents;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public async Task SaveCertificates(IEnumerable<ProductionCertificateCreated> events)
    {
        var productionCertificates = events
            .GroupBy(e => new { e.ShieldedGSRN.Value, e.Period.DateFrom, e.Period.DateTo })
            .Select(g => g.First())
            .Select(e =>
            {
                var cert = new ProductionCertificate(
                    gridArea: e.GridArea,
                    period: e.Period,
                    technology: e.Technology,
                    meteringPointOwner: e.MeteringPointOwner,
                    gsrn: e.ShieldedGSRN.Value,
                    quantity: e.ShieldedQuantity.Value,
                    blindingValue: e.BlindingValue);

                cert.SetId(e.CertificateId);
                cert.Issue();
                return cert;
            })
            .ToArray();

        //var productionCertificates = events.Select(e =>
        //{
        //    var cert = new ProductionCertificate(
        //        gridArea: e.GridArea,
        //        period: e.Period,
        //        technology: e.Technology,
        //        meteringPointOwner: e.MeteringPointOwner,
        //        gsrn: e.ShieldedGSRN.Value,
        //        quantity: e.ShieldedQuantity.Value,
        //        blindingValue: e.BlindingValue);

        //    cert.SetId(e.CertificateId);
        //    cert.Issue();
        //    return cert;
        //}).ToArray();

        await using var dbContext = await factory.CreateDbContextAsync();
        dbContext.ProductionCertificates.AddRange(productionCertificates);
        await dbContext.SaveChangesAsync();

        //foreach (var e in events)
        //{
        //    var cert = new ProductionCertificate(e.GridArea, e.Period, e.Technology, e.MeteringPointOwner,
        //        e.ShieldedGSRN.Value, e.ShieldedQuantity.Value, e.BlindingValue);
        //    cert.SetId(e.CertificateId);
        //    cert.Issue();
        //}
    }
}
