using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.MeasurementsSyncer.Persistence;

public interface ISlidingWindowState
{
    Task<MeteringPointTimeSeriesSlidingWindow> GetSlidingWindowStartTime(MeteringPointSyncInfo syncInfo, CancellationToken cancellationToken);
    Task UpsertSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<MeteringPointTimeSeriesSlidingWindow> meteringPointTimeSeriesSlidingWindows);
    IQueryable<MeteringPointTimeSeriesSlidingWindow> Query();
}
