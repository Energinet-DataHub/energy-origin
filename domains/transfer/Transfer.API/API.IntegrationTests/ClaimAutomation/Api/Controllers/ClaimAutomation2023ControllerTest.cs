using System;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.ClaimAutomation.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class ClaimAutomation2030ControllerTest
{
    private readonly string sub = Guid.NewGuid().ToString();
    //private readonly string tin = "12345678";
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public ClaimAutomation2030ControllerTest(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task StopProcess_WhenClaimProcessExists_ReturnsNoContent()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        await client.PostAsync("api/claim-automation/start", null);
        var result = await client.DeleteAsync("api/claim-automation/stop");
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StopProcess_WhenNoClaimProcessExists_ReturnsNotFound()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.DeleteAsync("api/claim-automation/stop");
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartProcess_WhenNoClaimProcessHasStarted_ReturnsCreatedAt()
    {
        var subject = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.PostAsync("api/claim-automation/start", null);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task StartProcess_WhenClaimProcessAlreadyStarted_ReturnsOk()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        await client.PostAsync("api/claim-automation/start", null);

        var result = await client.PostAsync("api/claim-automation/start", null);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimAutomationArguments_WhenClaimAutomationArgumentsExists_ReturnsOK()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        await client.PostAsync("api/claim-automation/start", null);

        var result = await client.GetAsync("api/claim-automation/");
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimAutomationArguments_WhenClaimAutomationArgumentsDoesNotExists_ReturnsNotFound()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetAsync("api/claim-automation/");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
