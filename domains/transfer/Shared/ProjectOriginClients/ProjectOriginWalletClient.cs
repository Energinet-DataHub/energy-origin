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

namespace ProjectOriginClients;

public interface IProjectOriginWalletClient
{
    Task<ResultList<GranularCertificate>?> GetGranularCertificates(Guid ownerSubject, CancellationToken cancellationToken);
    Task<ClaimResponse> ClaimCertificates(Guid ownerSubject, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
    Task<TransferResponse> TransferCertificates(Guid ownerSubject, GranularCertificate certificate, uint quantity, Guid receiverId);
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
            HashedAttributes = new string[] {}
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("v1/claims", content);
        res.EnsureSuccessStatusCode();

        if (res == null || res.Content == null)
            throw new HttpRequestException("Failed to transfer certificate.");

        return (await res.Content.ReadFromJsonAsync<TransferResponse>())!;
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
