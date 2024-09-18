using DataContext.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Mocks;

public sealed class MeasurementsWireMock : IDisposable
{
    private readonly WireMockServer server;

    private readonly Technology defaultTechnology = new Technology(
        AibFuelCode: "F01040100",
        AibTechCode: "T010000"
    );

    public MeasurementsWireMock() => server = WireMockServer.Start();

    public string Url => server.Url!;

    public void SetupMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType type, Technology? technology)> meteringPoints)
    {
        server.ResetMappings();
        var responseJson = BuildMeteringPointsResponse(meteringPoints);
        server
            .Given(Request.Create().WithPath("/api/measurements/meteringpoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    public void SetupMeteringPointsResponse(string gsrn, MeteringPointType type, Technology? technology = null)
    {
        server.ResetMappings();

        var responseJson = BuildMeteringPointsResponse([(gsrn, type, technology ?? defaultTechnology)]);
        server
            .Given(Request.Create().WithPath("/api/measurements/meteringpoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    private string BuildMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType type, Technology? technology)> meteringPoints)
    {
        var result = meteringPoints.Select(mp => new
        {
            mp.gsrn,
            gridArea = "DK1",
            mp.type,
            technology = mp.technology ?? defaultTechnology,
            canBeUsedForIssuingCertificates = true
        });

        return JsonSerializer.Serialize(new { Result = result }, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
    }

    public void Dispose() => server.Dispose();
}
