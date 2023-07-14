using System.Threading.Tasks;

namespace API.Data;

public interface IWalletDepositEndpointService
{
    Task<string> CreateWalletDepositWithToken(string issuer, string audience, string subject, string name, int expirationMinutes = 5);
}
