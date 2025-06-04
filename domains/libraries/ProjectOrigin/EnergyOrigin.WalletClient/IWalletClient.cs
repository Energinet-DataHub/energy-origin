using EnergyOrigin.WalletClient.Models;

namespace EnergyOrigin.WalletClient;

public interface IWalletClient
{
    Task<CreateWalletResponse> CreateWalletAsync(Guid ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<WalletRecord>> GetWalletsAsync(Guid ownerSubject, CancellationToken cancellationToken);
    Task<CreateExternalEndpointResponse> CreateExternalEndpointAsync(Guid ownerSubject, WalletEndpointReference walletEndpointReference, string textReference, CancellationToken cancellationToken);
    Task<RequestStatus> GetRequestStatusAsync(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken);
    Task<WalletEndpointReference> CreateWalletEndpointAsync(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken);
    Task<TransferResponse> TransferCertificatesAsync(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId, CancellationToken cancellationToken);
    Task<ClaimResponse> ClaimCertificatesAsync(Guid ownerSubject, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity, CancellationToken cancellationToken);
    Task<ResultList<GranularCertificate>?> GetGranularCertificatesAsync(Guid ownerSubject, CancellationToken cancellationToken, int? limit, int skip = 0, CertificateType? certificateType = null);
    Task<DisableWalletResponse> DisableWalletAsync(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken);
    Task<EnableWalletResponse> EnableWalletAsync(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<Claim>?> GetClaimsAsync(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken);
    Task<ResultList<Claim>?> GetClaimsAsync(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, TimeMatch timeMatch, CancellationToken cancellationToken);
}

