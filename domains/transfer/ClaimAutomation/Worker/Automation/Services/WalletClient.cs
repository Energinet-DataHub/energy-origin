using System;
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
using Microsoft.IdentityModel.Tokens;
using ProjectOriginClients.Models;

namespace ClaimAutomation.Worker.Automation.Services;

public interface IWalletClient
{
    Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken);
    Task<ClaimResponse> ClaimCertificates(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
}

public class WalletClient : IWalletClient
{
    private readonly HttpClient client;

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public WalletClient(HttpClient client)
    {
        this.client = client;
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
