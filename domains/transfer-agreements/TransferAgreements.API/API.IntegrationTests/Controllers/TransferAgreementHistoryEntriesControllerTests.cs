using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Controllers;

[UsesVerify]
public class TransferAgreementHistoryEntriesControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>, IClassFixture<WalletContainer>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;
    private readonly string sub;

    public TransferAgreementHistoryEntriesControllerTests(TransferAgreementsApiWebApplicationFactory factory,
        WalletContainer wallet)
    {
        this.factory = factory;
        factory.WalletUrl = wallet.WalletUrl;
        sub = Guid.NewGuid().ToString();
        authenticatedClient = factory.CreateAuthenticatedClient(sub);
    }

    [Fact]
    public async Task Create_ShouldGenerateHistoryEntry_WhenTransferAgreementIsCreated()
    {
        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345456",
            Some.Base64EncodedWalletDepositEndpoint
        );

        var createRequest = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var auditsResponse = await authenticatedClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(auditsResponse, settings);
    }

    [Fact]
    public async Task Edit_ShouldGenerateHistoryEntry_WhenEndDateIsUpdated()
    {
        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678",
            Some.Base64EncodedWalletDepositEndpoint
        );

        var createResponse = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createResponse.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newEndDate = new DateTimeOffset(2125, 5, 5, 5, 5, 5, TimeSpan.Zero).ToUnixTimeSeconds();
        var editRequest = new EditTransferAgreementEndDate(newEndDate);
        await authenticatedClient.PatchAsync($"api/transfer-agreements/{createdTransferAgreement.Id}", JsonContent.Create(editRequest));

        var auditsResponse = await authenticatedClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(auditsResponse, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldReturnNoContent_WhenNotOwnerOrReceiver()
    {
        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678",
            Some.Base64EncodedWalletDepositEndpoint
        );

        var createRequest = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "66332211");
        var response = await newAuthenticatedClient.GetAsync($"api/history/transfer-agreements/{createdTransferAgreement.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldContainActorNameField_WhenSender()
    {
        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678",
            Some.Base64EncodedWalletDepositEndpoint
        );

        var createRequest = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var otherActor = Guid.NewGuid();
        var otherActorClient = factory.CreateAuthenticatedClient(sub: sub, actor: otherActor.ToString());

        var senderResponse = await otherActorClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(senderResponse, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldNotContainActorNameField_WhenReceiver()
    {
        var transferAgreement = new CreateTransferAgreement(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            "12345678",
            Some.Base64EncodedWalletDepositEndpoint
        );

        var createRequest = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);

        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var receiver = Guid.NewGuid();
        var receiverClient = factory.CreateAuthenticatedClient(sub: receiver.ToString(), tin: "12345678");

        var receiverResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(receiverResponse, settings);
    }
}
