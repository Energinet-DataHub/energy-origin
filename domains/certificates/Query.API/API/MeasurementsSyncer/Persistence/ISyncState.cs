using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.MeasurementsSyncer.Persistence;

public interface ISyncState
{
    Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo, CancellationToken cancellationToken);

    Task SetSyncPosition(string gsrn, long syncedTo, CancellationToken cancellationToken);

    Task<MeteringPointTimeSeriesSlidingWindow?> GetMeteringPointSlidingWindow(string gsrn, CancellationToken cancellationToken);

    Task UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow, CancellationToken cancellationToken);



}
