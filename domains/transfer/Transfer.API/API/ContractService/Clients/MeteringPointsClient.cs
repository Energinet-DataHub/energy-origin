using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
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
        SetAuthorizationHeader();

        ValidateOwnerAndSubjectMatch(owner);

        return await httpClient.GetFromJsonAsync<MeteringPointsResponse>("/api/measurements/meteringpoints",
            cancellationToken: cancellationToken, options: jsonSerializerOptions);
    }

    private void SetAuthorizationHeader()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new HttpRequestException($"No HTTP context found. {nameof(MeteringPointsClient)} must be used as part of a request");

        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(httpContext.Request.Headers.Authorization!);
        httpClient.DefaultRequestHeaders.Add("EO_API_VERSION", "20240110");
    }

    private void ValidateOwnerAndSubjectMatch(string owner)
    {
        var user = new UserDescriptor(httpContextAccessor.HttpContext!.User);
        var subject = user.Subject.ToString();
        if (!owner.Equals(subject, StringComparison.InvariantCultureIgnoreCase))
            throw new HttpRequestException("Owner must match subject");
    }
}

public record MeteringPointsResponse(List<MeteringPoint> Result);

public record MeteringPoint(string Gsrn, string GridArea, MeterType Type, Address Address, Technology Technology, bool CanBeUsedForIssuingCertificates);

public enum MeterType { Consumption, Production, Child }

public record Technology(string AibFuelCode, string AibTechCode);

public record Address(string Address1, string? Address2, string? Locality, string City, string PostalCode,
    string Country);
