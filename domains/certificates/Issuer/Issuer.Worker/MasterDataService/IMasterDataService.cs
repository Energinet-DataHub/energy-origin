namespace Issuer.Worker.MasterDataService;

public interface IMasterDataService
{
    MasterData? GetMasterData(string gsrn);
}