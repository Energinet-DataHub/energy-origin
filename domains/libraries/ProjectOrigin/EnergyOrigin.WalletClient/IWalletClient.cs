using EnergyOrigin.WalletClient.Models;

namespace EnergyOrigin.WalletClient;

public interface IWalletClient
{
    Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken);
    Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference, string textReference, CancellationToken cancellationToken);
    Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken);
    Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken);
    Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId, CancellationToken cancellationToken);
    Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity, CancellationToken cancellationToken);
    Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken, int? limit, int skip = 0, CertificateType? certificateType = null);
    Task<DisableWalletResponse> DisableWallet(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken);
    Task<EnableWalletResponse> EnableWallet(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<Claim>?> GetClaims(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken);
    Task<ResultList<Claim>?> GetClaims(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, TimeMatch timeMatch, CancellationToken cancellationToken);
}

