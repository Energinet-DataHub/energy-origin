using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.WalletClient.Models;
using Microsoft.AspNetCore.Http;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace EnergyOrigin.WalletClient;

public class WalletClient : IWalletClient
{
    private readonly HttpClient client;
    private readonly IHttpContextAccessor httpContextAccessor;
    private const string WalletOwnerHeader = "wallet-owner";

    public WalletClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        this.client = client;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<CreateWalletResponse> CreateWallet(string ownerSubject, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetOwnerHeader(ownerSubject);
        ValidateOwnerAndSubjectMatch(ownerSubject);

        var request = new CreateWalletRequest
        {
            PrivateKey = null
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("v1/wallets", content, cancellationToken);
        return await ParseResponse<CreateWalletResponse>(response, cancellationToken);
    }

    public async Task<ResultList<WalletRecord>> GetWallets(string ownerSubject, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetOwnerHeader(ownerSubject);
        ValidateOwnerAndSubjectMatch(ownerSubject);

        var response = await client.GetAsync("v1/wallets", cancellationToken);
        var dto = await ParseResponse<ResultList<WalletRecordDto>>(response, cancellationToken);
        return MapGetWalletsDto(dto);
    }

    private static ResultList<WalletRecord> MapGetWalletsDto(ResultList<WalletRecordDto> dto)
    {
        var result = new ResultList<WalletRecord>
        {
            Result = Enumerable.Select<WalletRecordDto, WalletRecord>(dto.Result, r => new WalletRecord
            {
                Id = r.Id,
                PublicKey = new Secp256k1Algorithm().ImportHDPublicKey(r.PublicKey)
            }).ToList(),
            Metadata = dto.Metadata
        };
        return result;
    }

    public async Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject,
        WalletEndpointReference walletEndpointReference,
        string textReference, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject.ToString());
        var request = new CreateExternalEndpointRequest
        {
            TextReference = textReference,
            WalletReference = new WalletEndpointReferenceDto(walletEndpointReference.Version,
                walletEndpointReference.Endpoint,
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

    public async Task<RequestStatus> GetRequestStatus(Guid ownerSubject, Guid requestId,
        CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject.ToString());
        var response = await client.GetAsync($"v1/request-status/{requestId}", cancellationToken);

        response.EnsureSuccessStatusCode();
        var responseObj = await response.Content.ReadFromJsonAsync<RequestStatusResponse>(cancellationToken);
        return responseObj!.Status;
    }

    public async Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject,
        CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetOwnerHeader(ownerSubject);
        ValidateOwnerAndSubjectMatch(ownerSubject);

        var response = await client.PostAsync($"v1/wallets/{walletId}/endpoints", null, cancellationToken);

        response.EnsureSuccessStatusCode();

        if (response == null || response.Content == null)
            throw new HttpRequestException("Failed to create wallet endpoint.");

        var dto = await ParseResponse<CreateWalletEndpointResponse>(response, cancellationToken);
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(dto.WalletReference.PublicKey);
        return new WalletEndpointReference(dto.WalletReference.Version, dto.WalletReference.Endpoint, hdPublicKey);
    }

    public async Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate,
        uint quantity, Guid receiverId)
    {
        SetOwnerHeader(ownerSubject.ToString());
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

    public async Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate,
        GranularCertificate productionCertificate, uint quantity)
    {
        SetOwnerHeader(ownerSubject.ToString());
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

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject,
        CancellationToken cancellationToken, int? limit,
        int skip = 0, CertificateType? certificateType = null)
    {
        SetOwnerHeader(ownerSubject.ToString());

        var requestUri = $"v1/certificates?skip={skip}&limit={limit}&sortBy=end&sort=desc";

        if (certificateType is not null)
        {
            requestUri = $"{requestUri}&type={certificateType}";
        }

        return await client.GetFromJsonAsync<ResultList<GranularCertificate>?>(requestUri, _jsonSerializerOptions,
            cancellationToken: cancellationToken);
    }

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    private async Task<T> ParseResponse<T>(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
    {
        if (responseMessage.Content is null)
        {
            throw new HttpRequestException("Null response");
        }

        responseMessage.EnsureSuccessStatusCode();
        return (await responseMessage.Content.ReadFromJsonAsync<T>(cancellationToken))!;
    }

    private void ValidateHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new HttpRequestException(
                $"No HTTP context found. {nameof(WalletClient)} must be used as part of a request");
        }
    }

    private void SetOwnerHeader(string owner)
    {
        client.DefaultRequestHeaders.Remove(WalletOwnerHeader);
        client.DefaultRequestHeaders.Add(WalletOwnerHeader, owner);
    }

    private void ValidateOwnerAndSubjectMatch(string owner)
    {
        var identityDescriptor = new IdentityDescriptor(httpContextAccessor);
        var accessDescriptor = new AccessDescriptor(identityDescriptor);
        if (!accessDescriptor.IsAuthorizedToOrganization(Guid.Parse(owner)))
        {
            throw new HttpRequestException("Owner must match subject");
        }
    }
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

public record CreateWalletEndpointResponse(WalletEndpointReferenceDto WalletReference);

public record WalletEndpointReferenceDto(int Version, Uri Endpoint, byte[] PublicKey);

public record WalletEndpointReference(int Version, Uri Endpoint, IHDPublicKey PublicKey);

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
