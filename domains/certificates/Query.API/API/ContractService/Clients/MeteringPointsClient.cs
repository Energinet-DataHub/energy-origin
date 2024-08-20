using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
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

        var meteringPointsUrl = IsBearerTokenIssuedByB2C() ? $"/api/measurements/meteringpoints?organisationId={owner}" : "/api/measurements/meteringpoints";

        return await httpClient.GetFromJsonAsync<MeteringPointsResponse>(meteringPointsUrl, jsonSerializerOptions,
            cancellationToken);
    }

    private void SetAuthorizationHeader()
    {
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext!.Request.Headers.Authorization!);
    }

    private void SetApiVersionHeader()
    {
        var downstreamApiVersion = GetDownstreamApiVersion();
        httpClient.DefaultRequestHeaders.Remove("X-API-Version");
        httpClient.DefaultRequestHeaders.Add("X-API-Version", downstreamApiVersion);
    }

    private string GetDownstreamApiVersion()
    {
        if (IsBearerTokenIssuedByB2C())
        {
            return "20240515";
        }
        return "20240110";
    }

    private void ValidateHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new HttpRequestException($"No HTTP context found. {nameof(WalletClient)} must be used as part of a request");
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
        else
        {
            var user = new UserDescriptor(httpContextAccessor.HttpContext!.User);
            var subject = user.Subject.ToString();
            if (!owner.Equals(subject, StringComparison.InvariantCultureIgnoreCase))
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

public record MeteringPointsResponse(List<MeteringPoint> Result);

public record MeteringPoint(
    string Gsrn,
    string GridArea,
    MeterType Type,
    Address Address,
    Technology Technology,
    bool CanBeUsedForIssuingCertificates);

public enum MeterType
{
    Consumption,
    Production,
    Child
}

public record Technology(string AibFuelCode, string AibTechCode);

public record Address(
    string Address1,
    string? Address2,
    string? Locality,
    string City,
    string PostalCode,
    string Country);
