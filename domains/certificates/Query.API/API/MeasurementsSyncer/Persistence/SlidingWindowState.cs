using System;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using Microsoft.Extensions.Options;

namespace API.MeasurementsSyncer.Persistence;

public class SlidingWindowState : ISlidingWindowState
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptions<MeasurementsSyncOptions> _measurementsSyncOptions;

    public SlidingWindowState(ApplicationDbContext dbContext)
    {
        _measurementsSyncOptions = Options.Create(new MeasurementsSyncOptions());
        _dbContext = dbContext;
    }

    public async Task<MeteringPointTimeSeriesSlidingWindow> GetSlidingWindowStartTime(MeteringPointSyncInfo syncInfo,
        CancellationToken cancellationToken)
    {
        var existingSlidingWindow = await GetMeteringPointSlidingWindow(syncInfo.Gsrn, cancellationToken);
        var contractStartTime = UnixTimestamp.Create(syncInfo.StartSyncDate).RoundToNextHour();

        if (existingSlidingWindow is not null)
        {
            var pos = UnixTimestamp.Max(existingSlidingWindow.SynchronizationPoint, contractStartTime);
            existingSlidingWindow.UpdateTo(pos);
            return existingSlidingWindow;
        }

        var minimumAgeInHours = _measurementsSyncOptions.Value.MinimumAgeInHours;
        var initialSynchronizationPoint = UnixTimestamp.Now().Add(TimeSpan.FromHours(-minimumAgeInHours)).RoundToLatestHour();
        initialSynchronizationPoint = UnixTimestamp.Max(contractStartTime, initialSynchronizationPoint);

        return MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, initialSynchronizationPoint);
    }

    private async Task<MeteringPointTimeSeriesSlidingWindow?> GetMeteringPointSlidingWindow(Gsrn gsrn, CancellationToken cancellationToken)
    {
        return await _dbContext.MeteringPointTimeSeriesSlidingWindows.FindAsync(gsrn.Value, cancellationToken);
    }

    public async Task UpsertSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow, CancellationToken cancellationToken)
    {
        if (IsTracked(slidingWindow))
        {
            _dbContext.Update(slidingWindow);
        }
        else
        {
            await _dbContext.MeteringPointTimeSeriesSlidingWindows.AddAsync(slidingWindow, cancellationToken);
        }
    }

    private bool IsTracked(MeteringPointTimeSeriesSlidingWindow slidingWindow)
    {
        return _dbContext.Set<MeteringPointTimeSeriesSlidingWindow>().Local.Contains(slidingWindow);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
