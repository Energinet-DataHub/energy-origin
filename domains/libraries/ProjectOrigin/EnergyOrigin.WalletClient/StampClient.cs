using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using API.Configurations;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WalletClient;

public interface IStampClient
{
    Task<CreateRecipientResponse> CreateRecipient(WalletEndpointReference walletEndpointReference, CancellationToken cancellationToken);
    Task<IssueCertificateResponse> IssueCertificate(Guid recipientId, string meteringPointId, CertificateDto certificate, CancellationToken cancellationToken);
}

public class StampClient : IStampClient
{
    private readonly HttpClient client;
    private readonly ILogger<StampClient> logger;
    private readonly StampOptions options;

    public StampClient(HttpClient client, IOptions<StampOptions> options, ILogger<StampClient> logger)
    {
        this.client = client;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<CreateRecipientResponse> CreateRecipient(WalletEndpointReference walletEndpointReference, CancellationToken cancellationToken)
    {
        var request = new CreateRecipientRequest
        {
            WalletEndpointReference = new WalletEndpointReferenceDto(walletEndpointReference.Version, walletEndpointReference.Endpoint, walletEndpointReference.PublicKey.Export().ToArray())
        };
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("v1/recipients", content, cancellationToken);
        return await ParseResponse<CreateRecipientResponse>(response, cancellationToken);
    }

    public async Task<IssueCertificateResponse> IssueCertificate(Guid recipientId, string meteringPointId,
        CertificateDto certificate, CancellationToken cancellationToken)
    {
        var request = new CreateCertificateRequest
        {
            Certificate = certificate,
            MeteringPointId = meteringPointId,
            RecipientId = recipientId,
            RegistryName = options.RegistryName
        };

        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("v1/certificates", content, cancellationToken);
        return await ParseResponse<IssueCertificateResponse>(response, cancellationToken);
    }

    private async Task<T> ParseResponse<T>(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
    {
        if (responseMessage.Content is null)
        {
            throw new HttpRequestException("Null response");
        }

        if (!responseMessage.IsSuccessStatusCode)
        {
            var error = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Stamp error, StatusCode: {StatusCode} Content: {Content}", responseMessage.StatusCode, error);
            responseMessage.EnsureSuccessStatusCode();
        }
        return (await responseMessage.Content.ReadFromJsonAsync<T>(cancellationToken))!;
    }
}

public record CreateRecipientRequest
{
    public required WalletEndpointReferenceDto WalletEndpointReference { get; init; }
}

public record CreateRecipientResponse
{
    public required Guid Id { get; init; }
}

public record CreateCertificateRequest
{
    /// <summary>
    /// The recipient id of the certificate.
    /// </summary>
    public required Guid RecipientId { get; init; }

    /// <summary>
    /// The registry used to issues the certificate.
    /// </summary>
    public required string RegistryName { get; init; }

    /// <summary>
    /// The id of the metering point used to produce the certificate.
    /// </summary>
    public required string MeteringPointId { get; init; }

    /// <summary>
    /// The certificate to issue.
    /// </summary>
    public required CertificateDto Certificate { get; init; }
}

public record CertificateDto
{
    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The type of certificate (production or consumption).
    /// </summary>
    public required CertificateType Type { get; init; }

    /// <summary>
    /// The quantity available on the certificate.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The start of the period for which the certificate is valid.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end of the period for which the certificate is valid.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The Grid Area of the certificate.
    /// </summary>
    public required string GridArea { get; init; }

    /// <summary>
    /// Attributes of the certificate that is not hashed.
    /// </summary>
    public required Dictionary<string, string> ClearTextAttributes { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

public record HashedAttribute()
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}

public record IssueCertificateResponse() { }
