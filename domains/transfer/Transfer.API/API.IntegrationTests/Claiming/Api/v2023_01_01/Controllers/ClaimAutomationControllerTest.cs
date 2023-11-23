using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Claiming.Api.v2023_01_01.Controllers;

public class ClaimAutomationControllerTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public ClaimAutomationControllerTest(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task StopProcess_WhenClaimProcessExists_ReturnsNoContent()
    {
        var subject = Guid.NewGuid();
        await factory.SeedClaims(new List<ClaimAutomationArgument>()
        {
            new(subject, DateTimeOffset.UtcNow)
        });
        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

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

        await factory.SeedClaims(new List<ClaimAutomationArgument>()
        {
            new(subject, DateTimeOffset.UtcNow)
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.PostAsync("api/claim-automation/start", null);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimAutomationArguments_WhenClaimAutomationArgumentsExists_ReturnsOK()
    {
        var subject = Guid.NewGuid();

        var claimAutomationArgument = new ClaimAutomationArgument(subject, DateTimeOffset.UtcNow);

        await factory.SeedClaims(new List<ClaimAutomationArgument>()
        {
            claimAutomationArgument
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
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
