using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
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

        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldGetTransferAgreement_WhenIdIsValid()
    {
        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var transferAgreement = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "");

        var post = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        post.EnsureSuccessStatusCode();

        var postTransferAgreement = JsonConvert.DeserializeObject<TransferAgreement>(await post.Content.ReadAsStringAsync());

        var get = await authenticatedClient.GetAsync($"api/transfer-agreements/{postTransferAgreement.Id}");
        var getTransferAgreement = JsonConvert.DeserializeObject<TransferAgreement>(await get.Content.ReadAsStringAsync());

        postTransferAgreement.Should().BeEquivalentTo(getTransferAgreement);
    }

    [Fact]
    public async Task Get_ShouldReturnBadRequest_WhenIdIsInValid()
    {
        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var response = await authenticatedClient.GetAsync("api/transfer-agreements/1234");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenResourceIsNotFound()
    {
        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var response = await authenticatedClient.GetAsync($"api/transfer-agreements/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
