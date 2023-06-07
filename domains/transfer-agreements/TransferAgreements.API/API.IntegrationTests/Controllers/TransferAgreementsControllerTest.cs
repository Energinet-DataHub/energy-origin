using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class TransferAgreementsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public TransferAgreementsControllerTests(TransferAgreementsApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Create_ShouldCreateTransferAgreement_WhenModelIsValid()
    {
        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var transferAgreement = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "12345678");

        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_ShouldFail_WhenModelInvalid()
    {
        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var transferAgreement = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "");

        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBySubjectId_ShouldReturnTransferAgreements_WhenUserHasTransferAgreements()
    {
        var sub = Guid.NewGuid().ToString();
        await factory.SeedData(context =>
        {
            context.TransferAgreements.Add(new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                ActorId = "actor1",
                SenderId = Guid.Parse(sub),
                ReceiverTin = "12345678"
            });
            context.TransferAgreements.Add(new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow.AddDays(2),
                EndDate = DateTimeOffset.UtcNow.AddDays(3),
                ActorId = "actor2",
                SenderId = Guid.Parse(sub),
                ReceiverTin = "87654321"
            });
        });
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var response = await authenticatedClient.GetAsync("api/transfer-agreements");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadFromJsonAsync<List<TransferAgreementsResponse>>();
        transferAgreements.Should().NotBeNull();
        transferAgreements.Should().HaveCount(2);
    }


}
