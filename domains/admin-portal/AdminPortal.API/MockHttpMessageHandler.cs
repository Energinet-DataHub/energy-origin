using System.Net;
using System.Text;
using System.Text.Json;

namespace AdminPortal.API;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Random _random = new();
    private readonly List<string> _organizationIds = new();

    private readonly object _organizations;
    private readonly object _contracts;

    private int MockEntryCount { get; set; } = 600;

    public MockHttpMessageHandler()
    {
        InitializeOrganizations();

        var nameToTin = new Dictionary<string, string>();

        _organizations = Enumerable.Range(0, MockEntryCount)
            .Select(i =>
            {
                var orgName = GenerateCompanyName();
                if (nameToTin.TryGetValue(orgName, out var tin))
                    return new
                    {
                        OrganizationId = _organizationIds[i],
                        OrganizationName = orgName,
                        Tin = tin
                    };
                tin = GenerateTin();
                while (nameToTin.ContainsValue(tin))
                {
                    tin = GenerateTin();
                }
                nameToTin[orgName] = tin;
                return new
                {
                    OrganizationId = _organizationIds[i],
                    OrganizationName = orgName,
                    Tin = tin
                };
            })
            .ToList();

        _contracts = Enumerable.Range(0, MockEntryCount)
            .Select(i =>
            {
                var startDate = GenerateUnixTimestamp();
                var created = startDate - 604800;
                return new
                {
                    GSRN = GenerateGsrn(),
                    MeteringPointOwner = _organizationIds[i],
                    MeteringPointType = _random.Next(2) == 0 ? "Production" : "Consumption",
                    Created = created,
                    StartDate = startDate,
                    EndDate = GenerateEndDate()
                };
            })
            .ToList();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        if (request.RequestUri?.AbsolutePath.Contains("first-party-organizations") == true)
        {
            response = CreateJsonResponse(new { Result = _organizations });
        }
        else if (request.RequestUri?.AbsolutePath.Contains("internal-contracts") == true)
        {
            response = CreateJsonResponse(new { Result = _contracts });
        }
        else
        {
            response = new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return Task.FromResult(response);
    }

    private void InitializeOrganizations()
    {
        _organizationIds.Clear();
        for (var i = 0; i < MockEntryCount; i++)
        {
            _organizationIds.Add(Guid.NewGuid().ToString());
        }
    }

    private HttpResponseMessage CreateJsonResponse(object data)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json"
            )
        };
    }

    private string GenerateCompanyName()
    {
        var prefixes = new[] { "Team" };
        var suffixes = new[] { "Atlas", "Fusion", "Mosaic", "Apollo", "Data - Øko", "Heimdal", "Mandalorian", "Einstein", "Spektrum", "Raccoons", "Tværs", "Volt", "" };
        return $"{prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
    }

    private string GenerateTin()
    {
        return _random.Next(10000000, 99999999).ToString();
    }

    private string GenerateGsrn()
    {
        return $"57131313{_random.Next(1000000000):D10}";
    }

    private long GenerateUnixTimestamp()
    {
        var start = new DateTime(2020, 1, 1);
        var range = (DateTime.Today - start).Days;
        return new DateTimeOffset(start.AddDays(_random.Next(range))).ToUnixTimeSeconds();
    }

    private long? GenerateEndDate()
    {
        return _random.Next(2) == 0
            ? null
            : DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
    }
}
