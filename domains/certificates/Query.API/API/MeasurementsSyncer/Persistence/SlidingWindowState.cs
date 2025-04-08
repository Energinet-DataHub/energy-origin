using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.MeasurementsSyncer.Persistence;

public class SlidingWindowState : ISlidingWindowState
{
    private readonly ApplicationDbContext _dbContext;

    public SlidingWindowState(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void RemoveRange(IEnumerable<MeteringPointTimeSeriesSlidingWindow> meteringPointTimeSeriesSlidingWindows)
    {
        ArgumentNullException.ThrowIfNull(meteringPointTimeSeriesSlidingWindows);
        _dbContext.Set<MeteringPointTimeSeriesSlidingWindow>().RemoveRange(meteringPointTimeSeriesSlidingWindows);
    }


    public async Task<MeteringPointTimeSeriesSlidingWindow> GetSlidingWindowStartTime(MeteringPointSyncInfo syncInfo,
        CancellationToken cancellationToken)
    {
        var existingSlidingWindow = await GetMeteringPointSlidingWindow(syncInfo.Gsrn, cancellationToken);
        var contractStartTime = UnixTimestamp.Create(syncInfo.StartSyncDate).RoundToNextHour();

        if (existingSlidingWindow is not null)
        {
            var pos = UnixTimestamp.Max(existingSlidingWindow.SynchronizationPoint, contractStartTime);

            if (pos > existingSlidingWindow.SynchronizationPoint)
            {
                existingSlidingWindow.UpdateTo(pos);
            }

            return existingSlidingWindow;
        }

        return MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, contractStartTime);
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

    public IQueryable<MeteringPointTimeSeriesSlidingWindow> Query()
    {
        return _dbContext.MeteringPointTimeSeriesSlidingWindows.AsQueryable();
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
