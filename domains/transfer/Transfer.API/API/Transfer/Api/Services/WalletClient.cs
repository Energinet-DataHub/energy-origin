using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOriginClients.Models;

namespace ClaimAutomation.Worker.Automation.Services;

public interface IWalletClient
{
    Task<CreateWalletResponse> CreateWallet(string ownerSubject, CancellationToken cancellationToken);
    Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject, CancellationToken cancellationToken);
}

public class WalletClient : IWalletClient
{
    private readonly HttpClient client;
    private readonly IHttpContextAccessor httpContextAccessor;

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public WalletClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        this.client = client;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken)
    {
        SetupDummyAuthorizationHeader(ownerSubject.ToString());

        return await client.GetFromJsonAsync<ResultList<GranularCertificate>>("v1/certificates",
            cancellationToken: cancellationToken, options: jsonSerializerOptions);
    }

    public async Task<ClaimResponse> ClaimCertificates(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity)
    {
        SetupDummyAuthorizationHeader(ownerId.ToString());
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
            throw new HttpRequestException("Failed to create wallet.");

        return (await res.Content.ReadFromJsonAsync<ClaimResponse>())!;
    }

    public async Task<CreateWalletResponse> CreateWallet(string ownerSubject, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetAuthorizationHeader();
        ValidateOwnerAndSubjectMatch(ownerSubject);

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

    public async Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetAuthorizationHeader();
        ValidateOwnerAndSubjectMatch(ownerSubject);

        var res = await client.PostAsync($"v1/wallets/{walletId}/endpoints", null);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to create wallet endpoint.");

        var response = (await res.Content.ReadFromJsonAsync<CreateWalletEndpointResponse>())!;
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(response.WalletReference.PublicKey);
        return new WalletEndpointReference(response.WalletReference.Version, response.WalletReference.Endpoint, hdPublicKey);
    }

    private void ValidateHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new HttpRequestException($"No HTTP context found. {nameof(WalletClient)} must be used as part of a request");
    }

    private void SetAuthorizationHeader()
    {
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext!.Request.Headers.Authorization!);
    }

    private void ValidateOwnerAndSubjectMatch(string ownerSubject)
    {
        var user = new UserDescriptor(httpContextAccessor.HttpContext!.User);
        var subject = user.Subject.ToString();
        if (!ownerSubject.Equals(subject, StringComparison.InvariantCultureIgnoreCase))
            throw new HttpRequestException("Owner must match subject");
    }

    private void SetupDummyAuthorizationHeader(string ownerSubject)
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
    /// The random R used to generate the pedersen commitment with the quantity.
    /// </summary>
    public required byte[] RandomR { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

/// <summary>
/// Hashed attribute with salt.
/// </summary>
public record HashedAttribute()
{

    /// <summary>
    /// The key of the attribute.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The value of the attribute.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// The salt used to hash the attribute.
    /// </summary>
    public required byte[] Salt { get; init; }
}

public record ReceiveResponse() { }

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

/// <summary>
/// A response to a claim request.
/// </summary>
public record ClaimResponse()
{
    /// <summary>
    /// The id of the claim request.
    /// </summary>
    public required Guid ClaimRequestId { get; init; }
}
