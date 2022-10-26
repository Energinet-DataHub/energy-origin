using System.Threading.Tasks;

namespace Issuer.Worker.MasterDataService;

public interface IMasterDataService
{
    Task<MasterData?> GetMasterData(string gsrn);
}
