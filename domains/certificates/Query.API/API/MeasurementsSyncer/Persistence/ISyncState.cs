using System.Threading.Tasks;

namespace API.MeasurementsSyncer.Persistence;

public interface ISyncState
{
    Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo);
    Task SetSyncPosition(string gsrn, long syncedTo);
}
