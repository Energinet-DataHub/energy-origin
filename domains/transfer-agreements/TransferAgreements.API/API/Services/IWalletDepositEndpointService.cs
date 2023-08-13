using System;
using System.Threading.Tasks;

namespace API.Services;
public interface IWalletDepositEndpointService
{
    Task<string> CreateWalletDepositEndpoint(string bearerToken);
    Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
}
