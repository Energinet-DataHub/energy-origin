using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Newtonsoft.Json;
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
        var senderTin = "11223344";
        var receiverTin = "11223344";
        await factory.SeedData(context =>
        {
            context.TransferAgreements.Add(new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                ActorId = "actor1",
                SenderId = Guid.NewGuid(),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin
            });
            context.TransferAgreements.Add(new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow.AddDays(2),
                EndDate = DateTimeOffset.UtcNow.AddDays(3),
                ActorId = "actor2",
                SenderId = Guid.Parse(sub),
                SenderName = "Producent A/S",
                SenderTin = senderTin,
                ReceiverTin = "87654321"
            });
        });
        var authenticatedClient = factory.CreateAuthenticatedClient(sub, tin: receiverTin);

        var response = await authenticatedClient.GetAsync("api/transfer-agreements");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var t = JsonConvert.DeserializeObject<TransferAgreementsResponse>(transferAgreements);

        t.Should().NotBeNull();
        t.Result.Should().HaveCount(2);
    }


}
