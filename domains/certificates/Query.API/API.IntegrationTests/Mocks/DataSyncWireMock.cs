using System;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace API.IntegrationTests.Mocks;

public sealed class DataSyncWireMock : IDisposable
{
    private readonly WireMockServer server;

    public DataSyncWireMock() => server = WireMockServer.Start();

    public string Url => server.Url!;

    public void SetupMeteringPointsResponse(string gsrn, string type = "production")
    {
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn, type)));
    }

    private static string BuildMeteringPointsResponse(string gsrn, string type)
        => "{\"meteringPoints\":[{\"gsrn\": \"" + gsrn + "\",\"gridArea\": \"DK1\",\"type\": \"" + type + "\"}]}";

    public void Dispose() => server.Dispose();
}
