using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new ArgumentException("No HTTP context found. Client must be used as part of a request", nameof(httpContextAccessor));

        var headersAuthorization = httpContext.Request.Headers.Authorization;
        this.httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(headersAuthorization);
    }

    public async Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken)
    {
        var subject = httpContextAccessor.HttpContext?.User.FindFirstValue("subject") ?? string.Empty;
        if (!owner.Equals(subject, StringComparison.InvariantCultureIgnoreCase))
            throw new HttpRequestException("Owner must match subject");

        return await httpClient.GetFromJsonAsync<MeteringPointsResponse>("meteringPoints",
            cancellationToken: cancellationToken, options: jsonSerializerOptions);
    }
}

public record MeteringPointsResponse(List<MeteringPoint> MeteringPoints);

public record MeteringPoint(string GSRN, string GridArea, MeterType Type, Address Address);

public enum MeterType { Consumption, Production, Child }

public record Address(string Address1, string? Address2, string? Locality, string City, string PostalCode,
    string Country);
