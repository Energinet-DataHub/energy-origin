using System;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;

namespace API.MeasurementsSyncer.Persistence;

public class SlidingWindowState : ISlidingWindowState
{
    private readonly ApplicationDbContext dbContext;

    public SlidingWindowState(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<MeteringPointTimeSeriesSlidingWindow> GetSlidingWindowStartTime(MeteringPointSyncInfo syncInfo, CancellationToken cancellationToken)
    {
        var existingSlidingWindow = await GetMeteringPointSlidingWindow(syncInfo.GSRN, cancellationToken);

        if (existingSlidingWindow != null)
        {
            var pos = Math.Max(existingSlidingWindow.SynchronizationPoint.Seconds, UnixTimestamp.Create(syncInfo.StartSyncDate).Seconds);

            if (pos > existingSlidingWindow.SynchronizationPoint.Seconds)
            {
                existingSlidingWindow.UpdateTo(UnixTimestamp.Create(pos));
            }
        }
        else
        {
            existingSlidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.GSRN, UnixTimestamp.Create(syncInfo.StartSyncDate));
        }

        return existingSlidingWindow;
    }

    private async Task<MeteringPointTimeSeriesSlidingWindow?> GetMeteringPointSlidingWindow(string gsrn, CancellationToken cancellationToken)
    {
        var slidingWindow = await dbContext.MeteringPointTimeSeriesSlidingWindows.FindAsync(gsrn);
        return slidingWindow;
    }

    public async Task UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow, CancellationToken cancellationToken)
    {
        var existingWindow = await GetMeteringPointSlidingWindow(slidingWindow.GSRN, cancellationToken);
        if (existingWindow is null)
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Add(slidingWindow);
        }
        else
        {
            dbContext.MeteringPointTimeSeriesSlidingWindows.Update(slidingWindow);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
