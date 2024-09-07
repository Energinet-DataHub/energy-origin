using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOriginClients.Models;

namespace API.ContractService.Clients;

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
            Result = dto.Result.Select(r => new WalletRecord
            {
                Id = r.Id,
                PublicKey = new Secp256k1Algorithm().ImportHDPublicKey(r.PublicKey)
            }).ToList(),
            Metadata = dto.Metadata
        };
        return result;
    }

    public async Task<WalletEndpointReference> CreateWalletEndpoint(Guid walletId, string ownerSubject, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetOwnerHeader(ownerSubject);
        ValidateOwnerAndSubjectMatch(ownerSubject);

        var response = await client.PostAsync($"v1/wallets/{walletId}/endpoints", null, cancellationToken);
        var dto = await ParseResponse<CreateWalletEndpointResponse>(response, cancellationToken);
        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(dto.WalletReference.PublicKey);
        return new WalletEndpointReference(dto.WalletReference.Version, dto.WalletReference.Endpoint, hdPublicKey);
    }

    public async Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken, int? limit,
        int skip = 0)
    {
        SetOwnerHeader(ownerSubject.ToString());

        return await client.GetFromJsonAsync<ResultList<GranularCertificate>?>($"v1/certificates?skip={skip}&limit={limit}&sortBy=end&sort=desc", _jsonSerializerOptions,
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
            throw new HttpRequestException($"No HTTP context found. {nameof(WalletClient)} must be used as part of a request");
        }
    }

    private void SetOwnerHeader(string owner)
    {
        client.DefaultRequestHeaders.Remove(WalletOwnerHeader);
        client.DefaultRequestHeaders.Add(WalletOwnerHeader, owner);
    }

    private void ValidateOwnerAndSubjectMatch(string owner)
    {
        if (IsBearerTokenIssuedByB2C())
        {
            var identityDescriptor = new IdentityDescriptor(httpContextAccessor);
            var accessDescriptor = new AccessDescriptor(identityDescriptor);
            if (!accessDescriptor.IsAuthorizedToOrganization(Guid.Parse(owner)))
            {
                throw new HttpRequestException("Owner must match subject");
            }
        }
    }

    private bool IsBearerTokenIssuedByB2C()
    {
        return IdentityDescriptor.IsSupported(httpContextAccessor.HttpContext!);
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
