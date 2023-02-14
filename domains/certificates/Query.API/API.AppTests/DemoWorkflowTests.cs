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

        var response = await client.PostAsJsonAsync("api/certificates/demo", new DemoRequestModel { Foo = "bar" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
