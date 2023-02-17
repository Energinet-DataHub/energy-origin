using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Infrastructure;
using API.DemoWorkflow;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

public class DemoWorkflowTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public DemoWorkflowTests(QueryApiWebApplicationFactory factory, MartenDbContainer marten)
    {
        this.factory = factory;
        this.factory.MartenConnectionString = marten.ConnectionString;
    }

    [Fact]
    public async Task StartWorkflow()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var demoCreateResponse = await client.PostAsJsonAsync("api/certificates/demo", new DemoRequestModel { Foo = "bar" });

        demoCreateResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var statusLocation = demoCreateResponse.Headers.Location!;
        var statusResponse = await client.GetAsync(statusLocation);

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await statusResponse.Content.ReadFromJsonAsync<DemoStatusResponse>();
        content!.Status.Should().Be("Processing");

        var completedStatus = await client.RepeatedlyGetUntil<DemoStatusResponse>(statusLocation.ToString(),
            r => !r.Status.Equals("Processing", StringComparison.InvariantCultureIgnoreCase));
        completedStatus.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task NoMatchingWorkflow()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var unknownCorrelationId = Guid.NewGuid();
        var statusResponse = await client.GetAsync($"api/certificates/demo/status/{unknownCorrelationId}");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
