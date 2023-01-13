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

    public SignupTests(QueryApiWebApplicationFactory factory/*, MartenDbContainer martenDbContainer*/) //Leaving out Marten for now as it takes some time to load and we need to agree on the api interface first
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSomething()
    {
        var datasyncUrl = "http://localhost:8000/";
        using var wireMockServer = WireMockServer.Start(datasyncUrl);

        wireMockServer
            .Given(Request.Create().WithPath("/meteringPoints"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"meteringPoints\":[{\"gsrn\": \"123456789012345678\",\"gridArea\": \"DK1\",\"type\": \"production\"}]}"));
        
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = "123456789012345678", startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };
        
        var response = await client.PostAsJsonAsync("api/signup", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
