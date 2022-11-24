using System.Collections.Generic;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using API.MasterDataService;

namespace API.DataSyncSyncer.Persistence;

public interface ISyncState
{
    void SetNextPeriodStartTime(List<DataSyncDto> measurements, string GSRN);
    Task<long?> GetPeriodStartTime(MasterData masterData);
}
