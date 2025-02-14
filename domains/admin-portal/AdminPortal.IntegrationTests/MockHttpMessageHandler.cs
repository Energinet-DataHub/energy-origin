using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AdminPortal.IntegrationTests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public int MockEntryCount { get; set; } = 100;
    private readonly List<string> _organizationIds = new();
    private HttpResponseMessage? _firstPartyResponse;
    private HttpResponseMessage? _contractsResponse;
    private Exception? _simulatedException;

    public MockHttpMessageHandler()
    {
        InitializeOrganizations();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_simulatedException != null)
            throw _simulatedException;

        if (_firstPartyResponse != null &&
            request.RequestUri?.AbsolutePath.Contains("first-party-organizations") == true)
            return Task.FromResult(_firstPartyResponse);

        if (_contractsResponse != null &&
            request.RequestUri?.AbsolutePath.Contains("internal-contracts") == true)
            return Task.FromResult(_contractsResponse);

        var path = request.RequestUri?.AbsolutePath ?? "";

        return Task.FromResult(path switch
        {
            { } p when p.Contains("first-party-organizations") => CreateOrganizationsResponse(),
            { } p when p.Contains("internal-contracts") => CreateContractsResponse(),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
    }

    private HttpResponseMessage CreateOrganizationsResponse()
    {
        var organizations = Enumerable.Range(0, MockEntryCount)
            .Select(i => new
            {
                OrganizationId = _organizationIds[i],
                OrganizationName = GenerateCompanyName(),
                Tin = GenerateTin()
            });

        return CreateResponse(HttpStatusCode.OK, new { Result = organizations });
    }

    private HttpResponseMessage CreateContractsResponse()
    {
        var contracts = Enumerable.Range(0, MockEntryCount)
            .Select(i => new
            {
                GSRN = GenerateGsrn(),
                MeteringPointOwner = _organizationIds[i],
                MeteringPointType = Random.Shared.Next(2) == 0 ? "Production" : "Consumption",
                Created = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EndDate = (long?)null
            });

        return CreateResponse(HttpStatusCode.OK, new { Result = contracts });
    }

    public void SetFirstPartyApiResponse(HttpStatusCode statusCode, object? content = null)
        => _firstPartyResponse = CreateResponse(statusCode, content);

    public void SetContractsApiResponse(HttpStatusCode statusCode, object? content = null)
        => _contractsResponse = CreateResponse(statusCode, content);

    public void SimulateNetworkFailure(Exception exception)
        => _simulatedException = exception;

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, object? content)
        => new(statusCode)
        {
            Content = content != null
                ? new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
                : null
        };

    private void InitializeOrganizations()
    {
        _organizationIds.Clear();
        for (var i = 0; i < MockEntryCount; i++)
        {
            _organizationIds.Add(Guid.NewGuid().ToString());
        }
    }

    private static string GenerateCompanyName() => $"Test Org {Random.Shared.Next(1000)}";
    private static string GenerateTin() => Random.Shared.Next(10000000, 99999999).ToString();
    private static string GenerateGsrn() => $"57131313{Random.Shared.Next(1000000000):D10}";
}
