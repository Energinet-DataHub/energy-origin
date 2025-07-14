using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Http;

namespace API.ContractService.Clients;

public class MeteringPointsClient : IMeteringPointsClient
{
    private readonly HttpClient httpClient;
    private readonly IHttpContextAccessor httpContextAccessor;

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public MeteringPointsClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        this.httpClient = httpClient;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetAuthorizationHeader();
        ValidateOwnerAndSubjectMatch(owner);
        SetApiVersionHeader();

        var meteringPointsUrl = $"/api/measurements/meteringpoints?organizationId={owner}";

        return await httpClient.GetFromJsonAsync<MeteringPointsResponse>(meteringPointsUrl, jsonSerializerOptions, cancellationToken);
    }

    private void SetApiVersionHeader()
    {
        httpClient.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);
    }

    private void SetAuthorizationHeader()
    {
        httpClient.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext!.Request.Headers.Authorization!);
    }

    private void ValidateHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new HttpRequestException($"No HTTP context found. {nameof(MeteringPointsClient)} must be used as part of a request");
        }
    }

    private void ValidateOwnerAndSubjectMatch(string owner)
    {
        if (IdentityDescriptor.IsSupported(httpContextAccessor.HttpContext!))
        {
            var identityDescriptor = new IdentityDescriptor(httpContextAccessor);
            var accessDescriptor = new AccessDescriptor(identityDescriptor);
            if (!accessDescriptor.IsAuthorizedToOrganization(Guid.Parse(owner)))
            {
                throw new HttpRequestException("Owner must match subject");
            }
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
