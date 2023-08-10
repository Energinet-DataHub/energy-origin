using System;
using System.Threading.Tasks;
using API.Data;
using ProjectOrigin.WalletSystem.V1;

namespace API.Services;
public interface IWalletDepositEndpointService
{
    Task<string> CreateWalletDepositWithToken(JwtToken token);
    Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
}
