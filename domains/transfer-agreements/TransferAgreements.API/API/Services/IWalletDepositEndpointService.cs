using System;
using System.Threading.Tasks;
using API.Data;
using ProjectOrigin.WalletSystem.V1;

namespace API.Services;
public interface IWalletDepositEndpointService
{
    Task<string> CreateWalletDepositWithToken(JwtToken token);
    Task<WalletDepositEndpoint> GetWalletDepositEndpoint(string bearerToken);
    Task<ProjectOrigin.Common.V1.Uuid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
    Guid ConvertUuidToGuid(ProjectOrigin.Common.V1.Uuid receiverId);
}
