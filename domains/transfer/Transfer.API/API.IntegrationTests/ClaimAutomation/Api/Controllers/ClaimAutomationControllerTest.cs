using System;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.ClaimAutomation.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class ClaimAutomationControllerTest
{
    private readonly Guid sub = Guid.NewGuid();
    private readonly Guid orgId = Guid.NewGuid();

    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public ClaimAutomationControllerTest(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task StopProcess_WhenClaimProcessExists_ReturnsNoContent()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateB2CAuthenticatedClient(sub, orgId);
        await client.PostAsync($"api/claim-automation/start?organizationId={orgId}", null);
        var result = await client.DeleteAsync($"api/claim-automation/stop?organizationId={orgId}");
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StopProcess_WhenNoClaimProcessExists_ReturnsNotFound()
    {
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var result = await client.DeleteAsync($"api/claim-automation/stop?organizationId={orgId}");
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartProcess_WhenNoClaimProcessHasStarted_ReturnsCreatedAt()
    {
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var result = await client.PostAsync($"api/claim-automation/start?organizationId={orgId}", null);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task StartProcess_WhenClaimProcessAlreadyStarted_ReturnsOk()
    {
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId);

        await client.PostAsync($"api/claim-automation/start?organizationId={orgId}", null);

        var result = await client.PostAsync($"api/claim-automation/start?organizationId={orgId}", null);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimAutomationArguments_WhenClaimAutomationArgumentsExists_ReturnsOK()
    {
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId);

        await client.PostAsync($"api/claim-automation/start?organizationId={orgId}", null);

        var result = await client.GetAsync($"api/claim-automation?organizationId={orgId}");
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimAutomationArguments_WhenClaimAutomationArgumentsDoesNotExists_ReturnsNotFound()
    {
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var result = await client.GetAsync($"api/claim-automation?organizationId={orgId}");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
