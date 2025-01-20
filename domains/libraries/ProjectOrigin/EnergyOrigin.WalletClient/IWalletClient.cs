using ProjectOriginClients.Models;

namespace EnergyOrigin.WalletClient;

public interface IWalletClient
{
    Task<CreateWalletResponse> CreateWallet(string ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<WalletRecord>> GetWallets(string ownerSubject, CancellationToken cancellationToken);
    Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken, int? limit, int skip = 0);
}

