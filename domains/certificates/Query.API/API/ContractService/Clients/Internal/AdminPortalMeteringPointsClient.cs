using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace API.ContractService.Clients.Internal;

public class AdminPortalMeteringPointsClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IAdminPortalMeteringPointsClient
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public async Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken)
    {
        ValidateHttpContext();

        var meteringPointsUrl = $"/api/measurements/admin-portal/internal-meteringpoints?organizationId={owner}";

        return await httpClient.GetFromJsonAsync<MeteringPointsResponse>(meteringPointsUrl, jsonSerializerOptions, cancellationToken);
    }

    private void ValidateHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new HttpRequestException($"No HTTP context found. {nameof(MeteringPointsClient)} must be used as part of a request");
        }
    }
}

public record MeteringPointsResponse(List<MeteringPoint> Result);

public record MeteringPoint(
    string Gsrn,
    string GridArea,
    MeterType MeteringPointType,
    SubMeterType SubMeterType,
    Address Address,
    Technology Technology,
    string ConsumerCvr,
    bool CanBeUsedForIssuingCertificates,
    string Capacity,
    string BiddingZone);

public enum MeterType
{
    Consumption,
    Production,
    Child
}

public enum SubMeterType
{
    Physical,
    Virtual,
    Calculated
}

public record Technology(string AibFuelCode, string AibTechCode);

public record Address(
    string Address1,
    string? Address2,
    string? Locality,
    string City,
    string PostalCode,
    string Country);
