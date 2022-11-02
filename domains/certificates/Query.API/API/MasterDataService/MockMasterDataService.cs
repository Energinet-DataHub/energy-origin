using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.MasterDataService;

internal class MockMasterDataService : IMasterDataService
{
    private readonly Dictionary<string, MasterData> _data;

    public MockMasterDataService(MockMasterDataCollection collection) => _data = collection.Data.ToDictionary(d => d.GSRN, d => d);

    public Task<MasterData?> GetMasterData(string gsrn) => Task.FromResult(_data.ContainsKey(gsrn) ? _data[gsrn] : null);
}
