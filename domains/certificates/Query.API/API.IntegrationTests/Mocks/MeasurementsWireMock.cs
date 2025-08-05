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

namespace API.IntegrationTests.Mocks;

public sealed class MeasurementsWireMock : IDisposable
{
    private readonly WireMockServer server;

    private readonly API.ContractService.Clients.Technology defaultTechnology = new(
        AibFuelCode: "F01040100",
        AibTechCode: "T010000"
    );

    private readonly API.ContractService.Clients.Internal.Technology defaultInternalTechnology = new(
        AibFuelCode: "F01040100",
        AibTechCode: "T010000"
    );

    public MeasurementsWireMock() => server = WireMockServer.Start();

    public string Url => server.Url!;

    public void SetupMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType type, API.ContractService.Clients.Technology? technology, bool canBeUsedforIssuingCertificates)> meteringPoints)
    {
        server.ResetMappings();
        var responseJson = BuildMeteringPointsResponse(meteringPoints);
        server
            .Given(Request.Create().WithPath("/api/measurements/meteringpoints")
                .WithHeader(ApiVersions.HeaderName, ApiVersions.Version1))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    public void SetupMeteringPointsResponse(string gsrn, MeteringPointType type, API.ContractService.Clients.Technology? technology = null, bool canBeUsedforIssuingCertificates = true)
    {
        server.ResetMappings();

        var responseJson = BuildMeteringPointsResponse([(gsrn, type, technology ?? defaultTechnology, canBeUsedforIssuingCertificates)]);
        server
            .Given(Request.Create().WithPath("/api/measurements/meteringpoints")
                .WithHeader(ApiVersions.HeaderName, ApiVersions.Version1))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    public void SetupInternalMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType type, API.ContractService.Clients.Internal.Technology? technology, bool canBeUsedforIssuingCertificates)> meteringPoints)
    {
        server.ResetMappings();
        var responseJson = BuildInternalMeteringPointsResponse(meteringPoints);
        server
            .Given(Request.Create().WithPath("/api/measurements/admin-portal/internal-meteringpoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    public void SetupInternalMeteringPointsResponse(string gsrn, MeteringPointType type, API.ContractService.Clients.Internal.Technology? technology = null, bool canBeUsedforIssuingCertificates = true)
    {
        server.ResetMappings();

        var responseJson = BuildInternalMeteringPointsResponse([(gsrn, type, technology ?? defaultInternalTechnology, canBeUsedforIssuingCertificates)]);
        server
            .Given(Request.Create().WithPath("/api/measurements/admin-portal/internal-meteringpoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    private string BuildMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType meteringPointType, API.ContractService.Clients.Technology? technology, bool canBeUsedforIssuingCertificates)> meteringPoints)
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

    private string BuildInternalMeteringPointsResponse(
        IEnumerable<(string gsrn, MeteringPointType meteringPointType, API.ContractService.Clients.Internal.Technology? technology, bool canBeUsedforIssuingCertificates)> meteringPoints)
    {
        var result = meteringPoints.Select(mp => new
        {
            mp.gsrn,
            gridArea = "932",
            mp.meteringPointType,
            technology = mp.technology ?? defaultInternalTechnology,
            canBeUsedForIssuingCertificates = mp.canBeUsedforIssuingCertificates
        });

        return JsonSerializer.Serialize(new { Result = result }, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
    }

    public void Dispose() => server.Dispose();
}
