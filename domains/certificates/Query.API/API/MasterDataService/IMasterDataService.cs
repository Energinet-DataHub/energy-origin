using System.Threading.Tasks;

namespace API.MasterDataService;

public interface IMasterDataService
{
    Task<MasterData?> GetMasterData(string gsrn);
}
