using System;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace API.AppTests.Mocks;

public sealed class DataSyncWireMock : IDisposable
{
    private readonly WireMockServer server;

    public DataSyncWireMock(string url) => server = WireMockServer.Start(url);

    public void SetupMeteringPointsResponse(string gsrn, string type = "production") =>
        server
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn, type)));

    private static string BuildMeteringPointsResponse(string gsrn, string type)
        => "{\"meteringPoints\":[{\"gsrn\": \"" + gsrn + "\",\"gridArea\": \"DK1\",\"type\": \"" + type + "\"}]}";

    public void Dispose() => server.Dispose();
}