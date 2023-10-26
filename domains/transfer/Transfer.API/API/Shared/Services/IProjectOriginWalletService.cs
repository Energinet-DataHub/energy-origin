using System;
using System.Threading.Tasks;
using API.Transfer.Api.Models;
using Google.Protobuf.Collections;
using ProjectOrigin.WalletSystem.V1;

namespace API.Shared.Services;
public interface IProjectOriginWalletService
{
    Task<string> CreateWalletDepositEndpoint(string bearerToken);
    Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin);
    Task TransferCertificates(TransferAgreement transferAgreement);
    Task<RepeatedField<GranularCertificate>> GetGranularCertificates(Guid subjectId);
    Task ClaimCertificate(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
}
