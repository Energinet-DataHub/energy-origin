using System;
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
using Xunit;
using Newtonsoft.Json;

namespace API.IntegrationTests.Controllers;

public class TransferAgreementAuditsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;

    public TransferAgreementAuditsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_ShouldGenerateAudit_WhenTransferAgreementIsCreated()
    {
        var senderId = Guid.NewGuid();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var transferAgreement = new CreateTransferAgreement(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            "12345678"
        );

        var createResponse = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        createResponse.EnsureSuccessStatusCode();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdTransferAgreement = await createResponse.Content.ReadFromJsonAsync<TransferAgreementDto>();
        createdTransferAgreement.Should().NotBeNull();

        var auditsResponse = await authenticatedClient.GetAsync($"api/audits/transfer-agreements/{createdTransferAgreement.Id}");
        auditsResponse.EnsureSuccessStatusCode();

        auditsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var audits = await auditsResponse.Content.ReadFromJsonAsync<TransferAgreementAuditsResponse>();
        audits.Should().NotBeNull();
        audits.Result.Should().HaveCount(1);
    }







}
