using System.Collections.Generic;
using System.Threading.Tasks;
using API.MasterDataService;

namespace API.DataSyncSyncer.Service;

public interface IDataSyncProvider
{
    Task<List<MasterData>?> GetMasterData(string gsrn);
}
