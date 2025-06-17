using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnergyOrigin.WalletClient.Models;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace EnergyOrigin.WalletClient;

public class WalletClient(HttpClient client) : IWalletClient
{
    private const string WalletOwnerHeader = "wallet-owner";

    public async Task<CreateWalletResponse> CreateWalletAsync(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);

        var request = new CreateWalletRequest
        {
            PrivateKey = null
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("v1/wallets", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to create wallet {request}. Error: {error}", inner: null, statusCode: response.StatusCode);
        }

        return await ParseResponse<CreateWalletResponse>(response, cancellationToken);
    }

    public async Task<ResultList<WalletRecord>> GetWalletsAsync(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);

        var response = await client.GetAsync("v1/wallets", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to get wallets for owner {ownerSubject}. Error: {error}", inner: null, statusCode: response.StatusCode);
        }

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
                PublicKey = new Secp256k1Algorithm().ImportHDPublicKey(r.PublicKey),
                DisabledDate = r.DisabledDate
            }).ToList(),
            Metadata = dto.Metadata
        };
        return result;
    }

    public async Task<CreateExternalEndpointResponse> CreateExternalEndpointAsync(Guid ownerSubject,
        WalletEndpointReference walletEndpointReference,
        string textReference, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var request = new CreateExternalEndpointRequest
        {
            TextReference = textReference,
            WalletReference = new WalletEndpointReferenceDto(walletEndpointReference.Version,
                walletEndpointReference.Endpoint,
                walletEndpointReference.PublicKey.Export().ToArray())
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/external-endpoints", content, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to create externalendpoint {request}. Error: {error}", inner: null, statusCode: res.StatusCode);
        }
        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet external endpoint.");

        return await ParseResponse<CreateExternalEndpointResponse>(res, cancellationToken);
    }

    public async Task<RequestStatus> GetRequestStatusAsync(Guid ownerSubject, Guid requestId,
        CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var response = await client.GetAsync($"v1/request-status/{requestId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to get requestStatus {requestId}. Error: {error}", inner: null, statusCode: response.StatusCode);
        }
        var responseObj = await response.Content.ReadFromJsonAsync<RequestStatusResponse>(cancellationToken);
        return responseObj!.Status;
    }

    public async Task<WalletEndpointReference> CreateWalletEndpointAsync(Guid walletId, Guid ownerSubject,
        CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);

        var response = await client.PostAsync($"v1/wallets/{walletId}/endpoints", null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to create wallet endpoint. Error: {error}", inner: null, statusCode: response.StatusCode);
        }
        if (response == null || response.Content == null)
            throw new HttpRequestException("Failed to create wallet endpoint.");

        var dto = await ParseResponse<CreateWalletEndpointResponse>(response, cancellationToken);
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(dto.WalletReference.PublicKey);
        return new WalletEndpointReference(dto.WalletReference.Version, dto.WalletReference.Endpoint, hdPublicKey);
    }

    public async Task<TransferResponse> TransferCertificatesAsync(Guid ownerSubject, GranularCertificate certificate,
        uint quantity, Guid receiverId, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var request = new TransferRequest
        {
            CertificateId = certificate.FederatedStreamId,
            Quantity = quantity,
            ReceiverId = receiverId,
            HashedAttributes = new string[] { }
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/transfers", content, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to transfer {requestStr}. Error: {error}", inner: null, statusCode: res.StatusCode);
        }

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to transfer certificate.");

        return await ParseResponse<TransferResponse>(res, cancellationToken);
    }

    public async Task<ClaimResponse> ClaimCertificatesAsync(Guid ownerSubject, GranularCertificate consumptionCertificate,
        GranularCertificate productionCertificate, uint quantity, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var request = new ClaimRequest
        {
            ConsumptionCertificateId = consumptionCertificate.FederatedStreamId,
            ProductionCertificateId = productionCertificate.FederatedStreamId,
            Quantity = quantity
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/claims", content, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to claim {requestStr}. Error: {error}", inner: null, statusCode: res.StatusCode);
        }

        return await ParseResponse<ClaimResponse>(res, cancellationToken);
    }

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificatesAsync(Guid ownerSubject,
        CancellationToken cancellationToken, int? limit,
        int skip = 0, CertificateType? certificateType = null)
    {
        SetOwnerHeader(ownerSubject);

        var requestUri = $"v1/certificates?skip={skip}&limit={limit}&sortBy=end&sort=desc";

        if (certificateType is not null)
        {
            requestUri = $"{requestUri}&type={certificateType}";
        }

        var response = await client.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to get granular certificates. Error: {error}", inner: null, statusCode: response.StatusCode);
        }

        return await ParseResponse<ResultList<GranularCertificate>?>(response, cancellationToken);
    }

    public async Task<DisableWalletResponse> DisableWalletAsync(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);

        var response = await client.PostAsync($"v1/wallets/{walletId}/disable", null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to disable wallet with id {walletId}. Error: {error}", inner: null, statusCode: response.StatusCode);
        }

        return await ParseResponse<DisableWalletResponse>(response, cancellationToken);
    }

    public async Task<EnableWalletResponse> EnableWalletAsync(Guid walletId, Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var response = await client.PutAsync($"v1/wallets/{walletId}/enable", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to enable wallet with id {walletId}. Error: {error}", inner: null, statusCode: response.StatusCode);
        }
        return await ParseResponse<EnableWalletResponse>(response, cancellationToken);
    }

    public async Task<ResultList<Claim>?> GetClaimsAsync(Guid ownerSubject, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var requestUri = $"v1/claims?start={start?.ToUnixTimeSeconds()}&end={end?.ToUnixTimeSeconds()}";

        var response = await client.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to get claims. Error: {error}", inner: null, statusCode: response.StatusCode);
        }

        return await ParseResponse<ResultList<Claim>?>(response, cancellationToken);
    }

    public async Task<ResultList<Claim>?> GetClaimsAsync(
        Guid ownerSubject,
        DateTimeOffset? start,
        DateTimeOffset? end,
        TimeMatch timeMatch,
        CancellationToken cancellationToken)
    {
        SetOwnerHeader(ownerSubject);
        var url = new StringBuilder("v1/claims?")
            .Append($"start={start?.ToUnixTimeSeconds()}&")
            .Append($"end={end?.ToUnixTimeSeconds()}&")
            .Append($"timeMatch={timeMatch.ToString().ToLowerInvariant()}")
            .ToString();

        var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(message: $"Failed to get claims. Error: {error}", inner: null, statusCode: response.StatusCode);
        }

        return await ParseResponse<ResultList<Claim>?>(response, cancellationToken);
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

        return (await responseMessage.Content.ReadFromJsonAsync<T>(_jsonSerializerOptions, cancellationToken))!;
    }

    private void SetOwnerHeader(Guid owner)
    {
        client.DefaultRequestHeaders.Remove(WalletOwnerHeader);
        client.DefaultRequestHeaders.Add(WalletOwnerHeader, owner.ToString().ToLower());
    }
}

public record WalletRecord()
{
    public required Guid Id { get; init; }
    public required IHDPublicKey PublicKey { get; init; }
    public required long? DisabledDate { get; init; }
}

public record WalletRecordDto()
{
    public required Guid Id { get; init; }
    public required byte[] PublicKey { get; init; }
    public required long? DisabledDate { get; init; }
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

public record DisableWalletResponse()
{
    public Guid WalletId { get; init; }
    public required long DisabledDate { get; init; }
}

public record EnableWalletResponse()
{
    public Guid WalletId { get; init; }
}

public record Claim()
{
    public required Guid ClaimId { get; init; }
    public required uint Quantity { get; init; }
    public required ClaimedCertificate ProductionCertificate { get; init; }
    public required ClaimedCertificate ConsumptionCertificate { get; init; }
    public required long UpdatedAt { get; init; }
}

public record ClaimedCertificate()
{
    public required FederatedStreamId FederatedStreamId { get; init; }
    public required long Start { get; init; }
    public required long End { get; init; }
    public required string GridArea { get; init; }
    public required Dictionary<string, string> Attributes { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeMatch
{
    Hourly,
    All
}

