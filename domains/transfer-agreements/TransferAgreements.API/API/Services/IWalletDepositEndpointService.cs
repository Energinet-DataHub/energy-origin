using System.Threading.Tasks;
using API.Data;

namespace API.Services;

public interface IWalletDepositEndpointService
{
    Task<string> CreateWalletDepositWithToken(JwtToken token);
}
