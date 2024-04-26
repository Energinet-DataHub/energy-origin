using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using ProjectOriginClients.Models;
using System.Threading;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using System.Linq;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

namespace ProjectOriginClients;

public interface IProjectOriginWalletClient
{
    Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken);
    Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
    Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId);
    Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken);
    Task<ResultList<WalletRecord>> GetWallets(Guid ownerSubject, CancellationToken cancellationToken);
    Task<WalletEndpointReference> CreateWalletEndpoint(Guid ownerSubject, Guid walletId, CancellationToken cancellationToken);
    Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference, string textReference, CancellationToken cancellationToken);
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

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetDummyAuthorizationHeader(ownerSubject.ToString());

        return await client.GetFromJsonAsync<ResultList<GranularCertificate>>("v1/certificates",
            cancellationToken: cancellationToken, options: jsonSerializerOptions);
    }

    public async Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity)
    {
        SetDummyAuthorizationHeader(ownerSubject.ToString());
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
        SetDummyAuthorizationHeader(ownerSubject.ToString());
        var request = new TransferRequest
        {
            CertificateId = certificate.FederatedStreamId,
            Quantity = quantity,
            ReceiverId = receiverId,
            HashedAttributes = new string[] { }
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/claims", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to transfer certificate.");

        return (await res.Content.ReadFromJsonAsync<TransferResponse>())!;
    }

    public async Task<CreateWalletResponse> CreateWallet(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetDummyAuthorizationHeader(ownerSubject.ToString());
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
        SetDummyAuthorizationHeader(ownerSubject.ToString());

        var response = await client.GetFromJsonAsync<ResultList<WalletRecordDto>>("/v1/wallets", cancellationToken);

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
        SetDummyAuthorizationHeader(ownerSubject.ToString());
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var res = await client.PostAsync($"v1/wallets/{walletId}/endpoints", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet endpoint.");

        var response = (await res.Content.ReadFromJsonAsync<CreateWalletEndpointResponse>())!;
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(response.WalletReference.PublicKey);
        return new WalletEndpointReference(response.WalletReference.Version, response.WalletReference.Endpoint, hdPublicKey);
    }

    public async Task<CreateExternalEndpointResponse> CreateExternalEndpoint(Guid ownerSubject, WalletEndpointReference walletEndpointReference, string textReference, CancellationToken cancellationToken)
    {
        SetDummyAuthorizationHeader(ownerSubject.ToString());
        var request = new CreateExternalEndpointRequest
        {
            TextReference = textReference,
            WalletReference = walletEndpointReference
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");
        var res = await client.PostAsync($"v1/external-endpoints", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet external endpoint.");

        return (await res.Content.ReadFromJsonAsync<CreateExternalEndpointResponse>())!;
    }

    private void SetDummyAuthorizationHeader(string ownerSubject)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateBearerToken(ownerSubject));
    }

    private string GenerateBearerToken(string owner)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", owner) }),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
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
    public required WalletEndpointReference WalletReference { get; init; }

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
