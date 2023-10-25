using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Claiming.Api.Dto.Response;
using API.Claiming.Api.Models;
using API.IntegrationTests.Factories;
using API.Shared.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Claiming.Controllers;

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
            new(subject)
        });
        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.DeleteAsync("api/claims/stop-claim-process");
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StopProcess_WhenNoClaimProcessExists_ReturnsNotFound()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.DeleteAsync("api/claims/stop-claim-process");
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartProcess_WhenNoClaimProcessHasStarted_ReturnsCreatedAt()
    {
        var subject = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.PostAsync("api/claims/start-claim-process", null);
        result.StatusCode.Should().Be(HttpStatusCode.Created);

    }

    [Fact]
    public async Task StartProcess_WhenClaimProcessAlreadyStarted_ReturnsOk()
    {
        var subject = Guid.NewGuid();

        await factory.SeedClaims(new List<ClaimSubject>()
        {
            new(subject)
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());

        var result = await client.PostAsync("api/claims/start-claim-process", null);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClaimSubjectHistory_WhenHistoryNotExist_ReturnsNotFound()
    {

        var subject = Guid.NewGuid();
        var historyEntry = new ClaimSubjectHistory()
        {
            ActorId = Guid.NewGuid().ToString(),
            ActorName = "Peter Producent",
            AuditAction = "Insert",
            CreatedAt = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            SubjectId = Guid.NewGuid()
        };
        await factory.SeedClaimSubjectHistory(new List<ClaimSubjectHistory>()
        {
            historyEntry
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetAsync("api/claims/claim-process-history");
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClaimSubjectHistory_WhenHistoryExists_ReturnsOK()
    {
        var subject = Guid.NewGuid();
        var historyEntry = new ClaimSubjectHistory()
        {
            ActorId = Guid.NewGuid().ToString(),
            ActorName = "Peter Producent",
            AuditAction = "Insert",
            CreatedAt = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            SubjectId = subject
        };
        await factory.SeedClaimSubjectHistory(new List<ClaimSubjectHistory>()
        {
            historyEntry
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetFromJsonAsync<ClaimSubjectHistoryEntriesDto>("api/claims/claim-process-history");

        result.Should().NotBeNull();
        result!.History.Should().HaveCount(1);
        result.History[0].ActorName.Should().Be(historyEntry.ActorName);
        result.History[0].CreatedAt.Should().BeSameDateAs(historyEntry.CreatedAt);
        result.History[0].AuditAction.Should().Be(historyEntry.AuditAction);
    }

    [Fact]
    public async Task GetClaimSubjects_WhenClaimSubjectsExists_ReturnsOK()
    {
        var subject = Guid.NewGuid();

        var claimSubject = new ClaimSubject(subject);

        await factory.SeedClaims(new List<ClaimSubject>()
        {
            claimSubject
        });

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetFromJsonAsync<ClaimSubject>("api/claims/claim-process");
        result.Should().Be(claimSubject);
    }

    [Fact]
    public async Task GetClaimSubjects_WhenClaimSubjectsDoesNotExists_ReturnsNotFound()
    {
        var subject = Guid.NewGuid();

        var client = factory.CreateAuthenticatedClient(sub: subject.ToString());
        var result = await client.GetAsync("api/claims/claim-process");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
