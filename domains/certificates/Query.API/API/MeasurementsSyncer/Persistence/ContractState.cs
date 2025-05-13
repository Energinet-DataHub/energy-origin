using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer.Persistence;

public class ContractState : IContractState
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly ILogger<ContractState> logger;
    private readonly MeasurementsSyncOptions options;

    public ContractState(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<ContractState> logger,
        IOptions<MeasurementsSyncOptions> options)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken = default)
    {
        return await GetSyncInfos(DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task DeleteContractAndSlidingWindow(Gsrn gsrn)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var contractsToDelete = dbContext.Contracts.Where(x => x.GSRN == gsrn.Value);
        dbContext.Contracts.RemoveRange(contractsToDelete);

        var slidingWindowsToDelete = dbContext.MeteringPointTimeSeriesSlidingWindows.Where(x => x.GSRN == gsrn.Value);
        dbContext.MeteringPointTimeSeriesSlidingWindows.RemoveRange(slidingWindowsToDelete);

        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(DateTimeOffset time, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var allContracts = await dbContext.Contracts.AsNoTracking().ToListAsync(cancellationToken);

            var minimumAgeThreshold = time.AddHours(-options.MinimumAgeThresholdHours);

            var eligibleContracts = allContracts
                .Where(c => c.StartDate < minimumAgeThreshold)
                .Where(c =>
                {
                    var slidingWindow = dbContext.MeteringPointTimeSeriesSlidingWindows.SingleOrDefault(sw => sw.GSRN == c.GSRN);

                    // Include if sliding window doesn't exist (first-time processing)
                    var noSlidingWindowExists = slidingWindow is null;

                    // Include if sliding window exists and has missing intervals
                    var hasMissingIntervals = slidingWindow is not null && slidingWindow.MissingMeasurements.Intervals.Any();

                    // Include if sliding window exists, and not synced all the way to the end
                    var notSyncedToEndDate = slidingWindow is not null &&
                                             (c.EndDate is null || slidingWindow.SynchronizationPoint < UnixTimestamp.Create(c.EndDate.Value));

                    return noSlidingWindowExists || hasMissingIntervals || notSyncedToEndDate;
                })
                .ToList();

            //TODO: Currently the sync is only per GSRN/metering point, but should be changed to a combination of (GSRN, metering point owner). See https://github.com/Energinet-DataHub/energy-origin-issues/issues/1659 for more details
            var grouped = eligibleContracts.GroupBy(c => c.GSRN);
            var singleOwner = new List<(string gsrn, CertificateIssuingContract oldest)>();
            var multiOwner = new List<string>();

            foreach (var g in grouped)
            {
                if (GetNumberOfOwners(g) == 1)
                {
                    var oldest = g.MinBy(c => c.StartDate)!;
                    singleOwner.Add((g.Key, oldest));
                }
                else
                {
                    multiOwner.Add(g.Key);
                }
            }

            if (multiOwner.Count > 0)
            {
                logger.LogWarning(
                    "Skipping sync of GSRN with multiple owners: {contractsWithChangingOwnerForSameMeteringPoint}",
                    multiOwner);
            }

            if (singleOwner.Count == 0)
                return new List<MeteringPointSyncInfo>();

            var gsrnKeyObjects = singleOwner
                .Select(x => new Gsrn(x.gsrn))
                .Distinct()
                .ToArray();

            var hashSetOfSponsoredContracts = new HashSet<string>(
                await dbContext.Sponsorships
                    .AsNoTracking()
                    .Where(s => gsrnKeyObjects.Contains(s.SponsorshipGSRN))
                    .Select(s => s.SponsorshipGSRN.Value)
                    .ToListAsync(cancellationToken),
                StringComparer.Ordinal);

            var syncInfos = singleOwner.Select(t =>
            {
                var oldest = t.oldest;
                var gsrn = t.gsrn;

                return new MeteringPointSyncInfo(
                    new Gsrn(gsrn),
                    oldest.StartDate,
                    oldest.EndDate,
                    oldest.MeteringPointOwner,
                    oldest.MeteringPointType,
                    oldest.GridArea,
                    oldest.RecipientId,
                    oldest.Technology,
                    hashSetOfSponsoredContracts.Contains(gsrn));
            }).ToList();

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
