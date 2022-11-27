using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.MasterDataService;

internal class MockMasterDataService : IMasterDataService
{
    private readonly Dictionary<string, MockMasterData> data;

    public MockMasterDataService(MockMasterDataCollection collection) => data = collection.Data.ToDictionary(d => d.GSRN, d => d);

    public Task<MasterData?> GetMasterData(string gsrn)
    {
        MasterData? result = null;
        var mockMasterData = data.ContainsKey(gsrn) ? data[gsrn] : null;
        if (mockMasterData == null)
            return Task.FromResult(result);

        var meteringPointOwner = "???";

        result = new MasterData(mockMasterData.GSRN, mockMasterData.GridArea, mockMasterData.Type,
            mockMasterData.Technology, meteringPointOwner, mockMasterData.MeteringPointOnboardedStartDate);

        return Task.FromResult(result)!;
    }
}
