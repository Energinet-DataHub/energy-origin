using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Claiming.Api.Dto.Response;
using API.Claiming.Api.Models;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Shared.Extensions;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Claiming.Api.Controllers;

public class ClaimControllerTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public ClaimControllerTest(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task StopProcess_WhenClaimProcessExists_ReturnsNoContent()
    {
        var subject = Guid.NewGuid();
        await factory.SeedClaims(new List<ClaimSubject>()
        {
            new(subject, DateTimeOffset.Now)
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

        await factory.SeedClaims(new List<ClaimSubject>()
        {
            new(subject, DateTimeOffset.Now)
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.PostAsync("api/claim-automation/start", null);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimSubjectHistory_WhenHistoryEntryHasDifferentSubjectId_ReturnsNotFound()
    {
        const string actor = "Peter Producent";
        var client1 = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), actor: actor);
        await client1.PostAsync("api/claim-automation/start", null);

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());
        var result = await client.GetAsync("api/claim-automation/history");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClaimSubjectHistory_WhenHistoryExists_ReturnsOK()
    {
        const string actor = "Peter Producent";
        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), actor: actor);
        await client.PostAsync("api/claim-automation/start", null);
        var result = await client
            .RepeatedlyGetUntil<ClaimSubjectHistoryEntriesDto>(
                "api/claim-automation/history",
                res => res.History.Any()
            );

        result.Should().NotBeNull();
        result.History[0].ActorName.Should().Be(actor);
    }

    [Fact]
    public async Task GetClaimSubjects_WhenClaimSubjectsExists_ReturnsOK()
    {
        var subject = Guid.NewGuid();

        var claimSubject = new ClaimSubject(subject, DateTimeOffset.Now);

        await factory.SeedClaims(new List<ClaimSubject>()
        {
            claimSubject
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetFromJsonAsync<ClaimSubject>("api/claim-automation/");
        result.Should().Be(claimSubject);
    }

    [Fact]
    public async Task GetClaimSubjects_WhenClaimSubjectsDoesNotExists_ReturnsNotFound()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetAsync("api/claim-automation/");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
