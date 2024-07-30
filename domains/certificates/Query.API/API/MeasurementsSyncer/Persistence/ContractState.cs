using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer.Persistence;

public class ContractState : IContractState
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly ILogger<ContractState> logger;

    public ContractState(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<ContractState> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }
    public async Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var allContracts = await dbContext.Contracts.AsNoTracking().ToListAsync(cancellationToken);
            var nonExpiredContracts = allContracts.Where(c => !c.IsExpired()).ToList();

            //TODO: Currently the sync is only per GSRN/metering point, but should be changed to a combination of (GSRN, metering point owner). See https://github.com/Energinet-DataHub/energy-origin-issues/issues/1659 for more details
            var syncInfos = nonExpiredContracts.GroupBy(c => c.GSRN)
                .Where(g => GetNumberOfOwners(g) == 1)
                .Select(g =>
                {
                    var oldestContract = g.OrderBy(c => c.StartDate).First();
                    var gsrn = g.Key;
                    return new MeteringPointSyncInfo(new Gsrn(gsrn), oldestContract.StartDate, oldestContract.MeteringPointOwner,
                        oldestContract.MeteringPointType, oldestContract.GridArea, oldestContract.RecipientId,
                        oldestContract.Technology);
                })
                .ToList();

            var contractsWithChangingOwnerForSameMeteringPoint = nonExpiredContracts.GroupBy(c => c.GSRN)
                .Where(g => GetNumberOfOwners(g) > 1);

            if (contractsWithChangingOwnerForSameMeteringPoint.Any())
            {
                logger.LogWarning("Skipping sync of GSRN with multiple owners: {contractsWithChangingOwnerForSameMeteringPoint}",
                    contractsWithChangingOwnerForSameMeteringPoint);
            }

            return syncInfos;
        }
        catch (Exception e)
        {
            logger.LogError("Failed fetching contracts. Exception: {e}", e);
            return new List<MeteringPointSyncInfo>();
        }
    }

    private static int GetNumberOfOwners(IGrouping<string, CertificateIssuingContract> g)
    {
        return g.Select(c => c.MeteringPointOwner).Distinct().Count();
    }
}
