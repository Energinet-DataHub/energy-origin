using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace API.Transfer.Api.Services;

public interface IWalletClient
{
    Task<CreateWalletResponse> CreateWallet(string ownerSubject, CancellationToken cancellationToken);
    Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject, CancellationToken cancellationToken);
    Task<CreateExternalEndpointResponse> CreateExternalEndpoint(string ownerSubject, WalletEndpointReference walletEndpointReference, string textReference, CancellationToken cancellationToken);
}

public class WalletClient : IWalletClient
{
    private readonly HttpClient client;
    private readonly IHttpContextAccessor httpContextAccessor;

    public WalletClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        this.client = client;
        this.httpContextAccessor = httpContextAccessor;
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

    public async Task<CreateExternalEndpointResponse> CreateExternalEndpoint(string ownerSubject, WalletEndpointReference walletEndpointReference, string textReference,
        CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetAuthorizationHeader();
        ValidateOwnerAndSubjectMatch(ownerSubject);

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
