using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.MeasurementsSyncer.Persistence;

public interface ISyncState
{
    Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo);

    Task SetSyncPosition(string gsrn, long syncedTo);

    Task<MeteringPointTimeSeriesSlidingWindow?> GetMeteringPointSlidingWindow(string gsrn);

    Task UpdateSlidingWindow(MeteringPointTimeSeriesSlidingWindow slidingWindow);

    Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken);

}
