using System.Collections.Generic;
using System.Linq;

namespace Issuer.Worker.MasterDataService;

public class MockMasterDataService : IMasterDataService 
{
    private readonly Dictionary<string, MasterData> data;

    public MockMasterDataService(MasterDataCollection collection) => data = collection.Data.ToDictionary(d => d.GSRN, d => d);

    public MasterData? GetMasterData(string gsrn) => data.ContainsKey(gsrn) ? data[gsrn] : null;
}
