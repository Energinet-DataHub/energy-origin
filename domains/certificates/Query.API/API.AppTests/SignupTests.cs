using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

public class SignupTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly MartenDbContainer marten;

    public SignupTests(QueryApiWebApplicationFactory factory, MartenDbContainer marten)
    {
        this.factory = factory;
        this.marten = marten;
        this.factory.MartenConnectionString = marten.ConnectionString;
    }

    [Fact]
    public async Task CreateSignUp_SignUpGsrn_Created()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "111111111111111111", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signup", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSignUp_GsrnAlreadyExistsInDb_Conflict()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "222222222222222222", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        var response = await client.PostAsJsonAsync("api/signup", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response = await client.PostAsJsonAsync("api/signup", body);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
