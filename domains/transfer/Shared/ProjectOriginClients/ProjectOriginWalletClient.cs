using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOriginClients.Models;

namespace ProjectOriginClients;

public interface IProjectOriginWalletClient
{
    Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken, int? limit, int skip = 0);

    Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate,
        uint quantity);

    Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId);

    Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken);

    Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken);

    Task<WalletEndpointReference> CreateWalletEndpoint(Guid ownerSubject, Guid walletId, CancellationToken cancellationToken);

    Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference,
        string textReference, CancellationToken cancellationToken);

    Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken);
}

public class ProjectOriginWalletClient : IProjectOriginWalletClient
{
    private readonly HttpClient client;

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public ProjectOriginWalletClient(HttpClient client)
    {
        this.client = client;
    }

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken, int? limit,
        int skip = 0)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());

        return await client.GetFromJsonAsync<ResultList<GranularCertificate>>($"v1/certificates?skip={skip}&limit={limit}",
            cancellationToken: cancellationToken, options: jsonSerializerOptions);
    }

    public async Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate,
        GranularCertificate productionCertificate, uint quantity)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());
        var request = new ClaimRequest
        {
            ConsumptionCertificateId = consumptionCertificate.FederatedStreamId,
            ProductionCertificateId = productionCertificate.FederatedStreamId,
            Quantity = quantity
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/claims", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to claim certificates.");

        return (await res.Content.ReadFromJsonAsync<ClaimResponse>())!;
    }

    public async Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());
        var request = new TransferRequest
        {
            CertificateId = certificate.FederatedStreamId,
            Quantity = quantity,
            ReceiverId = receiverId,
            HashedAttributes = new string[] { }
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/transfers", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to transfer certificate.");

        return (await res.Content.ReadFromJsonAsync<TransferResponse>())!;
    }

    public async Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());
        var request = new CreateWalletRequest
        {
            PrivateKey = null
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/wallets", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet.");

        return (await res.Content.ReadFromJsonAsync<CreateWalletResponse>())!;
    }

    public async Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());

        var response = await client.GetFromJsonAsync<ResultList<WalletRecordDto>>("v1/wallets", cancellationToken);

        if (response == null)
            throw new HttpRequestException("Failed to get wallets.");

        var result = new ResultList<WalletRecord>
        {
            Result = response.Result.Select(r => new WalletRecord
            {
                Id = r.Id,
                PublicKey = new Secp256k1Algorithm().ImportHDPublicKey(r.PublicKey)
            }).ToList(),
            Metadata = response.Metadata
        };
        return result;
    }

    public async Task<WalletEndpointReference> CreateWalletEndpoint(Guid ownerSubject, Guid walletId, CancellationToken cancellationToken)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());
        var res = await client.PostAsync($"v1/wallets/{walletId}/endpoints", null);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet endpoint.");

        var response = (await res.Content.ReadFromJsonAsync<CreateWalletEndpointResponse>())!;
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(response.WalletReference.PublicKey);
        return new WalletEndpointReference(response.WalletReference.Version, response.WalletReference.Endpoint, hdPublicKey);
    }

    public async Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference,
        string textReference, CancellationToken cancellationToken)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());
        var request = new CreateExternalEndpointRequest
        {
            TextReference = textReference,
            WalletReference = new WalletEndpointReferenceDto(walletEndpointReference.Version, walletEndpointReference.Endpoint,
                walletEndpointReference.PublicKey.Export().ToArray())
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/external-endpoints", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet external endpoint.");

        return (await res.Content.ReadFromJsonAsync<CreateExternalEndpointResponse>())!;
    }

    public async Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId, CancellationToken cancellationToken)
    {
        SetWalletOwnerHeader(ownerSubject.ToString());
        var response = await client.GetAsync($"v1/request-status/{requestId}", cancellationToken);

        response.EnsureSuccessStatusCode();
        var responseObj = await response.Content.ReadFromJsonAsync<RequestStatusResponse>(cancellationToken);
        return responseObj!.Status;
    }

    private void SetWalletOwnerHeader(string ownerSubject)
    {
        if (client.DefaultRequestHeaders.Contains("wallet-owner"))
        {
            client.DefaultRequestHeaders.Remove("wallet-owner");
        }

        client.DefaultRequestHeaders.Add("wallet-owner", ownerSubject);
    }
}

/// <summary>
/// A request to claim a production and consumption certificate.
/// </summary>
public record ClaimRequest()
{
    /// <summary>
    /// The id of the production certificate to claim.
    /// </summary>
    public required FederatedStreamId ProductionCertificateId { get; init; }

    /// <summary>
    /// The id of the consumption certificate to claim.
    /// </summary>
    public required FederatedStreamId ConsumptionCertificateId { get; init; }

    /// <summary>
    /// The quantity of the certificates to claim.
    /// </summary>
    public required uint Quantity { get; init; }
}

public record ClaimResponse()
{
    public required Guid ClaimRequestId { get; init; }
}

/// <summary>
/// A request to transfer a certificate to another wallet.
/// </summary>
public record TransferRequest()
{
    /// <summary>
    /// The federated stream id of the certificate to transfer.
    /// </summary>
    public required FederatedStreamId CertificateId { get; init; }

    /// <summary>
    /// The id of the wallet to transfer the certificate to.
    /// </summary>
    public required Guid ReceiverId { get; init; }

    /// <summary>
    /// The quantity of the certificate to transfer.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// List of hashed attributes to transfer with the certificate.
    /// </summary>
    public required string[] HashedAttributes { get; init; }
}

public record TransferResponse()
{
    public required Guid TransferRequestId { get; init; }
}

public record CreateWalletRequest()
{
    /// <summary>
    /// The private key to import. If not provided, a private key will be generated.
    /// </summary>
    public byte[]? PrivateKey { get; init; }
}

public record CreateWalletResponse()
{
    public Guid WalletId { get; init; }
}

public record WalletRecord()
{
    public required Guid Id { get; init; }
    public required IHDPublicKey PublicKey { get; init; }
}

public record WalletRecordDto()
{
    public required Guid Id { get; init; }
    public required byte[] PublicKey { get; init; }
}

public record CreateWalletEndpointResponse(WalletEndpointReferenceDto WalletReference);

public record WalletEndpointReferenceDto(int Version, Uri Endpoint, byte[] PublicKey);

public record WalletEndpointReference(int Version, Uri Endpoint, IHDPublicKey PublicKey);

/// <summary>
/// Request to create a new external endpoint.
/// </summary>
public record CreateExternalEndpointRequest()
{
    /// <summary>
    /// The wallet reference to the wallet, one wants to create a link to.
    /// </summary>
    public required WalletEndpointReferenceDto WalletReference { get; init; }

    /// <summary>
    /// The text reference for the wallet, one wants to create a link to.
    /// </summary>
    public required string TextReference { get; init; }
}

/// <summary>
/// Response to create a new external endpoint.
/// </summary>
public record CreateExternalEndpointResponse()
{
    /// <summary>
    /// The ID of the created external endpoint.
    /// </summary>
    public required Guid ReceiverId { get; init; }
}

/// <summary>
/// Request to receive a certificate-slice from another wallet.
/// </summary>
public record ReceiveRequest()
{
    /// <summary>
    /// The public key of the receiving wallet.
    /// </summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// The sub-position of the publicKey used on the slice on the registry.
    /// </summary>
    public required uint Position { get; init; }

    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required FederatedStreamId CertificateId { get; init; }

    /// <summary>
    /// The quantity of the slice.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The random R used to generate the pedersen commitment with the quantitiy.
    /// </summary>
    public required byte[] RandomR { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public record HashedAttribute()
{
    public required string Key { get; init; }
    public required string Value { get; init; }

    /// <summary>
    /// The salt used to hash the attribute.
    /// </summary>
    public required byte[] Salt { get; init; }
}

public enum RequestStatus
{
    Pending,
    Completed,
    Failed
}

/// <summary>
/// Request status response.
/// </summary>
public record RequestStatusResponse()
{
    /// <summary>
    /// The status of the request.
    /// </summary>
    public required RequestStatus Status { get; init; }
}
