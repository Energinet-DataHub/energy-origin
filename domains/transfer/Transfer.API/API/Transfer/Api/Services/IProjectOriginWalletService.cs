using System;
using System.Threading.Tasks;
using API.Transfer.Api.Models;

namespace API.Transfer.Api.Services;
public interface IProjectOriginWalletService
{
    Task<string> CreateWalletDepositEndpoint(string bearerToken);
    Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
    Task TransferCertificates(TransferAgreement transferAgreement);
}
