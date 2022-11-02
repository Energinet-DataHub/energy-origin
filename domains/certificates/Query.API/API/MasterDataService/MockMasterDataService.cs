using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.MasterDataService;

internal class MockMasterDataService : IMasterDataService
{
    private readonly Dictionary<string, MasterData> data;

    public MockMasterDataService(MockMasterDataCollection collection) => data = collection.Data.ToDictionary(d => d.GSRN, d => d);

    public Task<MasterData?> GetMasterData(string gsrn) => Task.FromResult(data.ContainsKey(gsrn) ? data[gsrn] : null);
}
