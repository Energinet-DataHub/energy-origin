using DataContext.ValueObjects;
using System;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Mocks;

public sealed class DataSyncWireMock : IDisposable
{
    private readonly WireMockServer server;
    private readonly Technology defaultTechnology = new Technology(
        AibFuelCode: "F01040100",
        AibTechCode: "T010000"
    );

    public DataSyncWireMock() => server = WireMockServer.Start();

    public string Url => server.Url!;

    public void SetupMeteringPointsResponse(string gsrn, MeteringPointType type, Technology? technology = null)
    {
        server.ResetMappings();
        var responseJson = BuildMeteringPointsResponse(gsrn, type, technology ?? defaultTechnology);
        server
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(responseJson));
    }

    private static string BuildMeteringPointsResponse(string gsrn, MeteringPointType type, Technology technology)
    {
        var technologyJson = JsonSerializer.Serialize(new
        {
            technology.AibFuelCode,
            technology.AibTechCode
        });

        return $"{{\"meteringPoints\":[{{\"gsrn\": \"{gsrn}\", \"gridArea\": \"DK1\", \"type\": \"{type}\", \"technology\": {technologyJson}}}]}}";
    }

    public void Dispose() => server.Dispose();
}
