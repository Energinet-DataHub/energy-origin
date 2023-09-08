using System.Threading.Tasks;

namespace API.DataSyncSyncer.Persistence;

public interface ISyncState
{
    Task<long?> GetPeriodStartTime(MeteringPointSyncInfo syncInfo);
    void SetSyncPosition(string gsrn, long syncedTo);
}
