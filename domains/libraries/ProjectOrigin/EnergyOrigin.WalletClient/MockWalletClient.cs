using System.Collections.Concurrent;
using EnergyOrigin.WalletClient.Models;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

namespace EnergyOrigin.WalletClient.Testing;

public class MockWalletClient : IWalletClient
{
    private readonly ConcurrentDictionary<Guid, WalletRecord> _wallets = new();
    private readonly ConcurrentDictionary<Guid, WalletEndpointReference> _walletEndpoints = new();
    private readonly ConcurrentDictionary<Guid, CreateExternalEndpointResponse> _externalEndpoints = new();
    private readonly ConcurrentDictionary<Guid, RequestStatus> _requestStatuses = new();
    private readonly ConcurrentDictionary<FederatedStreamId, GranularCertificate> _certificates = new(new FederatedStreamIdComparer());
    private readonly ConcurrentDictionary<Guid, ClaimResponse> _claims = new();
    private readonly ConcurrentDictionary<Guid, TransferResponse> _transfers = new();

    public List<(Guid OwnerId, Guid WalletId)> CreatedWallets { get; } = new();
    public List<(Guid OwnerId, Guid WalletId, Guid EndpointId)> CreatedEndpoints { get; } = new();
    public List<(Guid OwnerId, Guid ReceiverId, string TextReference)> CreatedExternalEndpoints { get; } = new();
    public List<(Guid OwnerId, FederatedStreamId ConsumptionId, FederatedStreamId ProductionId, uint Quantity)> ClaimedCertificates { get; } = new();
    public List<(Guid OwnerId, FederatedStreamId CertificateId, Guid ReceiverId, uint Quantity)> TransferredCertificates { get; } = new();

    public async Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken)
    {
        var walletId = Guid.NewGuid();
        var publicKey = new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter();

        var wallet = new WalletRecord
        {
            Id = walletId,
            PublicKey = publicKey
        };

        _wallets[walletId] = wallet;
        CreatedWallets.Add((ownerSubject, walletId));

        return await Task.FromResult(new CreateWalletResponse { WalletId = walletId });
    }

    public async Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken)
    {
        var wallets = _wallets.Values.ToList();

        return await Task.FromResult(new ResultList<WalletRecord>
        {
            Result = wallets,
            Metadata = new PageInfo
            {
                Count = wallets.Count,
                Offset = 0,
                Limit = 100,
                Total = wallets.Count
            }
        });
    }

    public async Task<CreateExternalEndpointResponse> CreateExternalEndpoint(
        Guid ownerSubject,
        WalletEndpointReference walletEndpointReference,
        string textReference,
        CancellationToken cancellationToken)
    {
        var receiverId = Guid.NewGuid();
        var response = new CreateExternalEndpointResponse
        {
            ReceiverId = receiverId
        };

        _externalEndpoints[receiverId] = response;
        CreatedExternalEndpoints.Add((ownerSubject, receiverId, textReference));

        return await Task.FromResult(response);
    }

    public async Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken)
    {
        return await Task.FromResult(_requestStatuses.GetValueOrDefault(requestId, RequestStatus.Pending));
    }

    public async Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
    {
        if (!_wallets.ContainsKey(walletId))
            throw new InvalidOperationException($"Wallet with ID {walletId} not found");

        var endpointId = Guid.NewGuid();
        var endpoint = new WalletEndpointReference(
            1,
            new Uri($"https://example.com/endpoints/{endpointId}"),
            _wallets[walletId].PublicKey
        );

        _walletEndpoints[endpointId] = endpoint;
        CreatedEndpoints.Add((ownerSubject, walletId, endpointId));

        return await Task.FromResult(endpoint);
    }

    public async Task<TransferResponse> TransferCertificates(
        Guid ownerSubject,
        GranularCertificate certificate,
        uint quantity,
        Guid receiverId)
    {
        if (!_certificates.TryGetValue(certificate.FederatedStreamId, out var existingCertificate))
            throw new InvalidOperationException($"Certificate {certificate.FederatedStreamId.StreamId} not found");

        if (existingCertificate.Quantity < quantity)
            throw new InvalidOperationException($"Insufficient quantity available. Requested: {quantity}, Available: {existingCertificate.Quantity}");

        existingCertificate.Quantity -= quantity;
        _certificates[certificate.FederatedStreamId] = existingCertificate;

        var transferRequestId = Guid.NewGuid();
        var response = new TransferResponse
        {
            TransferRequestId = transferRequestId
        };

        _transfers[transferRequestId] = response;
        _requestStatuses[transferRequestId] = RequestStatus.Pending;

        TransferredCertificates.Add((ownerSubject, certificate.FederatedStreamId, receiverId, quantity));

        return await Task.FromResult(response);
    }

    public async Task<ClaimResponse> ClaimCertificates(
        Guid ownerSubject,
        GranularCertificate consumptionCertificate,
        GranularCertificate productionCertificate,
        uint quantity)
    {
        if (!_certificates.TryGetValue(consumptionCertificate.FederatedStreamId, out var existingConsumption))
            throw new InvalidOperationException($"Consumption certificate {consumptionCertificate.FederatedStreamId.StreamId} not found");

        if (!_certificates.TryGetValue(productionCertificate.FederatedStreamId, out var existingProduction))
            throw new InvalidOperationException($"Production certificate {productionCertificate.FederatedStreamId.StreamId} not found");

        if (existingConsumption.Quantity < quantity || existingProduction.Quantity < quantity)
            throw new InvalidOperationException("Insufficient quantity available for claiming");

        if (existingConsumption.CertificateType != CertificateType.Consumption)
            throw new InvalidOperationException("First certificate must be a consumption certificate");

        if (existingProduction.CertificateType != CertificateType.Production)
            throw new InvalidOperationException("Second certificate must be a production certificate");

        existingConsumption.Quantity -= quantity;
        existingProduction.Quantity -= quantity;

        _certificates[consumptionCertificate.FederatedStreamId] = existingConsumption;
        _certificates[productionCertificate.FederatedStreamId] = existingProduction;

        var claimRequestId = Guid.NewGuid();
        var response = new ClaimResponse
        {
            ClaimRequestId = claimRequestId
        };

        _claims[claimRequestId] = response;
        _requestStatuses[claimRequestId] = RequestStatus.Pending;

        ClaimedCertificates.Add((ownerSubject, consumptionCertificate.FederatedStreamId, productionCertificate.FederatedStreamId, quantity));

        return await Task.FromResult(response);
    }

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(
        Guid ownerSubject,
        CancellationToken cancellationToken,
        int? limit,
        int skip = 0,
        CertificateType? certificateType = null)
    {
        var certificates = _certificates.Values
            .Where(c => certificateType == null || c.CertificateType == certificateType)
            .OrderByDescending(c => c.End)
            .Skip(skip)
            .Take(limit ?? 100)
            .ToList();

        return await Task.FromResult(new ResultList<GranularCertificate>
        {
            Result = certificates,
            Metadata = new PageInfo
            {
                Count = certificates.Count,
                Offset = skip,
                Limit = limit ?? 100,
                Total = _certificates.Count
            }
        });
    }

    public GranularCertificate AddTestCertificate(
        CertificateType type,
        uint quantity = 1000,
        string gridArea = "DK1",
        Dictionary<string, string>? attributes = null)
    {
        var streamId = Guid.NewGuid();
        var federatedId = new FederatedStreamId
        {
            Registry = "test-registry",
            StreamId = streamId
        };

        var certificate = new GranularCertificate
        {
            FederatedStreamId = federatedId,
            Quantity = quantity,
            Start = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds(),
            End = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(),
            GridArea = gridArea,
            CertificateType = type,
            Attributes = attributes ?? new Dictionary<string, string>
            {
                { "TechCode", type == CertificateType.Production ? "T070000" : "E000000" },
                { "FuelCode", type == CertificateType.Production ? "F00000000" : "" }
            }
        };

        _certificates[federatedId] = certificate;
        return certificate;
    }

    public void SetRequestStatus(Guid requestId, RequestStatus status)
    {
        _requestStatuses[requestId] = status;
    }


    public void Reset()
    {
        _wallets.Clear();
        _walletEndpoints.Clear();
        _externalEndpoints.Clear();
        _requestStatuses.Clear();
        _certificates.Clear();
        _claims.Clear();
        _transfers.Clear();

        CreatedWallets.Clear();
        CreatedEndpoints.Clear();
        CreatedExternalEndpoints.Clear();
        ClaimedCertificates.Clear();
        TransferredCertificates.Clear();
    }
}

internal class FederatedStreamIdComparer : IEqualityComparer<FederatedStreamId>
{
    public bool Equals(FederatedStreamId? x, FederatedStreamId? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;

        return x.Registry == y.Registry && x.StreamId == y.StreamId;
    }

    public int GetHashCode(FederatedStreamId obj)
    {
        return HashCode.Combine(obj.Registry, obj.StreamId);
    }
}
