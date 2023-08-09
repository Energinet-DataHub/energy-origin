using System.Threading.Tasks;

namespace API.Data;

public interface IWalletDepositEndpointService
{
    Task<string> CreateWalletDepositWithToken(JwtToken token);
}
