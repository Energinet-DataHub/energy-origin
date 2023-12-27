using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Transfer.Api.Models;
namespace API.Transfer.Api.Services;

public interface IProjectOriginWalletService
{
    Task<string> CreateWalletDepositEndpoint(AuthenticationHeaderValue bearerToken);
    Task<Guid> CreateReceiverDepositEndpoint(AuthenticationHeaderValue bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
}
