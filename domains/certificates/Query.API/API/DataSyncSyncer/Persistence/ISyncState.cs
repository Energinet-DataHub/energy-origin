using System.Threading.Tasks;
using API.MasterDataService;

namespace API.DataSyncSyncer.Persistence;

public interface ISyncState
{
    Task<long?> GetPeriodStartTime(MasterData masterData);
}
