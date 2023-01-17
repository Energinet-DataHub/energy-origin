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

public class SignupTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;
    private const string dataSyncUrl = "http://localhost:9001/";
    private const string validGsrn = "123456789012345678";

    public SignupTests(QueryApiWebApplicationFactory factory, MartenDbContainer marten)
    {
        this.factory = factory;

        factory.MartenConnectionString = marten.ConnectionString;
        factory.DataSyncUrl = dataSyncUrl;
    }

    [Fact]
    public async Task CreateSignup_SignUpGsrn_Created()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn: "111111111111111111")));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "111111111111111111", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSignup_GsrnAlreadyExistsInDb_Conflict()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn: "222222222222222222")));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "222222222222222222", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response = await client.PostAsJsonAsync("api/signUps", body);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateSignup_MeteringPointNotOwnedByUser_BadRequest()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn: "111111111111111111")));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSignup_MeteringPointIsConsumption_BadRequest()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(type: "consumption")));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSignup_InvalidGsrn_BadRequest()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse()));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "invalid GSRN", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerSignUps_QueryAllSignUps_Success()
    {
        using var dataSyncMock = WireMockServer.Start(dataSyncUrl);
        dataSyncMock
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(BuildMeteringPointsResponse(gsrn: "999999999999999999")));

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "999999999999999999", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };
        await client.PostAsJsonAsync("api/signUps", body);

        var response = await client.GetAsync("api/signUps");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

    }

    [Fact]
    public async Task GetAllMeteringPointOwnerSignUps_QueryAllSignUps_NotFound()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var response = await client.GetAsync("api/signUps");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static string BuildMeteringPointsResponse(string gsrn = validGsrn, string type = "production")
        => "{\"meteringPoints\":[{\"gsrn\": \"" + gsrn + "\",\"gridArea\": \"DK1\",\"type\": \"" + type + "\"}]}";
}
