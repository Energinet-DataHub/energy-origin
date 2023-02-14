using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using API.DemoWorkflow;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

public class DemoWorkflowTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public DemoWorkflowTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task DoesSomething()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var demoCreateResponse = await client.PostAsJsonAsync("api/certificates/demo", new DemoRequestModel { Foo = "bar" });

        demoCreateResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var statusLocation = demoCreateResponse.Headers.Location;

        var statusResponse = await client.GetAsync(statusLocation);
        statusResponse.StatusCode.Should().Be((HttpStatusCode)418);
    }
}
