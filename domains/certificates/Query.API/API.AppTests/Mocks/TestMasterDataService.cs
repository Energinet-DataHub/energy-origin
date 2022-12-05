using System.Collections.Generic;
using System.Threading.Tasks;
using API.MasterDataService;

namespace API.AppTests.Mocks;

public class TestMasterDataService : IMasterDataService
{
    private readonly Dictionary<string, MasterData> masterDatas = new();

    public Task<MasterData?> GetMasterData(string gsrn) =>
        Task.FromResult(masterDatas.ContainsKey(gsrn) ? masterDatas[gsrn] : null);

    public void Add(MasterData data) => masterDatas[data.GSRN] = data;
}
