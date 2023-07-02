using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.IntegrationTests.Factories;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Controllers;

[UsesVerify]
public class TransferAgreementHistoryEntriesControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;

    public TransferAgreementHistoryEntriesControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_ShouldGenerateHistoryEntry_WhenTransferAgreementIsCreated()
    {
        var senderId = Guid.NewGuid();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var transferAgreement = new CreateTransferAgreement(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            "12345678"
        );

        var createRequest = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var auditsResponse = await authenticatedClient.GetAsync($"api/history/transfer-agreements/{createdTransferAgreement.Id}");
        var audits = await auditsResponse.Content.ReadFromJsonAsync<TransferAgreementHistoryEntriesResponse>(JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMembersWithType(typeof(long));

        await Verifier.Verify(audits, settings);
    }

    [Fact]
    public async Task Edit_ShouldGenerateHistoryEntry_WhenEndDateIsUpdated()
    {
        var senderId = Guid.NewGuid();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678"
        );

        var createResponse = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createResponse.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newEndDate = new DateTimeOffset(2125, 5, 5, 5, 5, 5, TimeSpan.Zero).ToUnixTimeSeconds();
        var editRequest = new EditTransferAgreementEndDate(newEndDate);
        await authenticatedClient.PatchAsync($"api/transfer-agreements/{createdTransferAgreement.Id}", JsonContent.Create(editRequest));

        var auditsResponse = await authenticatedClient.GetAsync($"api/history/transfer-agreements/{createdTransferAgreement.Id}");
        var audits = await auditsResponse.Content.ReadFromJsonAsync<TransferAgreementHistoryEntriesResponse>(JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMembersWithType(typeof(string));
        settings.ScrubMember("AuditDate");

        await Verifier.Verify(audits, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldReturnNoContent_WhenNotOwnerOrReceiver()
    {
        var transferAgreementId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var receiverTin = "12345678";

        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin
            }
        });

        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "87654321");
        var response = await newAuthenticatedClient.GetAsync($"api/history/transfer-agreements/{transferAgreementId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldContainActorNameField_WhenSender()
    {
        var senderId = Guid.NewGuid();

        var creatingActor = Guid.NewGuid();
        var senderClient = factory.CreateAuthenticatedClient(sub: senderId.ToString(), actor: creatingActor.ToString());

        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678"
        );

        var createRequest = await senderClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var otherActor = Guid.NewGuid();
        var otherActorClient = factory.CreateAuthenticatedClient(sub: senderId.ToString(), actor: otherActor.ToString());

        var response = await otherActorClient.GetAsync($"api/history/transfer-agreements/{createdTransferAgreement.Id}");
        var senderAuditContent = await response.Content.ReadFromJsonAsync<TransferAgreementHistoryEntriesResponse>(JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMembersWithType(typeof(long));

        await Verifier.Verify(senderAuditContent, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldNotContainActorNameField_WhenReceiver()
    {
        var sender = Guid.NewGuid();
        var senderClient = factory.CreateAuthenticatedClient(sub: sender.ToString());

        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678"
        );

        var createRequest = await senderClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var receiver = Guid.NewGuid();
        var receiverClient = factory.CreateAuthenticatedClient(sub: receiver.ToString(), tin: "12345678");

        var response = await receiverClient.GetAsync($"api/history/transfer-agreements/{createdTransferAgreement.Id}");
        var receiverAuditContent = await response.Content.ReadFromJsonAsync<TransferAgreementHistoryEntriesResponse>(JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMembersWithType(typeof(long));

        await Verifier.Verify(receiverAuditContent, settings);
    }
}
