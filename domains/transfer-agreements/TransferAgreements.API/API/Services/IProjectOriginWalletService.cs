using System;
using System.Threading.Tasks;
using API.Data;
using API.Models;

namespace API.Services;
public interface IProjectOriginWalletService
{
    Task<string> CreateWalletDepositEndpoint(string bearerToken);
    Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
    Task TransferCertificates(TransferAgreement transferAgreement);
}
