using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.AppTests;

public class SignupTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;
    private const string dataSyncUrl = "http://localhost:9001/";
    private const string validGsrn = "123456789012345678";

    public SignupTests(QueryApiWebApplicationFactory factory/*, MartenDbContainer martenDbContainer*/) //Leaving out Marten for now as it takes some time to load and we need to agree on the api interface first
    {
        this.factory = factory;

        factory.DataSyncUrl = dataSyncUrl;
    }

    [Fact]
    public async Task GetSomething()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse()));
        
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };
        
        var response = await client.PostAsJsonAsync("api/signup", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task metering_point_not_owned_by_user()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn: "111111111111111111")));
        
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signup", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task metering_point_is_consumption()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(type: "consumption")));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signup", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task invalid_gsrn()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse()));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "invalid GSRN", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signup", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string BuildMeteringPointsResponse(string gsrn = validGsrn, string type="production")
        => "{\"meteringPoints\":[{\"gsrn\": \"" + gsrn + "\",\"gridArea\": \"DK1\",\"type\": \"" + type + "\"}]}";
}
