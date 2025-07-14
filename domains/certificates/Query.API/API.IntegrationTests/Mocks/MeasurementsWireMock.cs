using DataContext.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnergyOrigin.Setup;
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
        IEnumerable<(string gsrn, MeteringPointType type, Technology? technology, bool canBeUsedforIssuingCertificates)> meteringPoints)
    {
        server.ResetMappings();
        var responseJson = BuildMeteringPointsResponse(meteringPoints);
        server
            .Given(Request.Create().WithPath("/api/measurements/meteringpoints")
                .WithHeader(ApiVersions.HeaderName, ApiVersions.Version1))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    public void SetupMeteringPointsResponse(string gsrn, MeteringPointType type, Technology? technology = null, bool canBeUsedforIssuingCertificates = true)
    {
        server.ResetMappings();

        var responseJson = BuildMeteringPointsResponse([(gsrn, type, technology ?? defaultTechnology, canBeUsedforIssuingCertificates)]);
        server
            .Given(Request.Create().WithPath("/api/measurements/meteringpoints")
                .WithHeader(ApiVersions.HeaderName, ApiVersions.Version1))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    private string BuildMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType meteringPointType, Technology? technology, bool canBeUsedforIssuingCertificates)> meteringPoints)
    {
        var result = meteringPoints.Select(mp => new
        {
            mp.gsrn,
            gridArea = "932",
            mp.meteringPointType,
            technology = mp.technology ?? defaultTechnology,
            canBeUsedForIssuingCertificates = mp.canBeUsedforIssuingCertificates
        });

        return JsonSerializer.Serialize(new { Result = result }, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
    }

    public void Dispose() => server.Dispose();
}
