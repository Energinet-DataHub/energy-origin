using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using API.AppTests.Mocks;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

public sealed class SignUpTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>, IDisposable
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;
    private const string validGsrn = "123456789012345678";

    public SignUpTests(QueryApiWebApplicationFactory factory, MartenDbContainer marten)
    {
        const string dataSyncUrl = "http://localhost:9001/";
        dataSyncWireMock = new DataSyncWireMock(dataSyncUrl);

        factory.MartenConnectionString = marten.ConnectionString;
        factory.DataSyncUrl = dataSyncUrl;
        this.factory = factory;
    }

    [Fact]
    public async Task CreateSignUp_SignUpGsrn_Created()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: "111111111111111111");
        
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "111111111111111111", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdSignUpUri = response.Headers.Location;

        var createdSignUpResponse = await client.GetAsync(createdSignUpUri);

        createdSignUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSignUp_GsrnAlreadyExistsInDb_Conflict()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: "222222222222222222");

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "222222222222222222", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response = await client.PostAsJsonAsync("api/signUps", body);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateSignUp_MeteringPointNotOwnedByUser_BadRequest()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: "111111111111111111");

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSignUp_MeteringPointIsConsumption_BadRequest()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: validGsrn, type: "consumption");

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = validGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSignUp_InvalidGsrn_BadRequest()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: "111111111111111111");

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "invalid GSRN", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerSignUps_QueryAllSignUps_Success()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: "999999999999999999");

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "999999999999999999", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };
        await client.PostAsJsonAsync("api/signUps", body);

        var response = await client.GetAsync("api/signUps");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerSignUps_QueryAllSignUps_NoContent()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var response = await client.GetAsync("api/signUps");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSpecificSignUp_SignUpDoesNotExist_NotFound()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var signUpId = Guid.NewGuid().ToString();
        var response = await client.GetAsync($"api/signUps/{signUpId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSpecificSignUp_UserIsNotOwner_NotFound()
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: "333333333333333333");

        var subject1 = Guid.NewGuid().ToString();
        using var client1 = factory.CreateAuthenticatedClient(subject1);

        var body = new { gsrn = "333333333333333333", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client1.PostAsJsonAsync("api/signUps", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdSignUpUri = response.Headers.Location;

        var subject2 = Guid.NewGuid().ToString();
        using var client2 = factory.CreateAuthenticatedClient(subject2);

        var getSpecificSignUpResponse = await client2.GetAsync(createdSignUpUri);

        getSpecificSignUpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    public void Dispose() => dataSyncWireMock.Dispose();
}
