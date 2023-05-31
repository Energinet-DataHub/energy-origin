using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.Data;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class TransferAgreementsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public TransferAgreementsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_ShouldCreateTransferAgreement_WhenModelIsValid()
    {

        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var transferAgreement = new CreateTransferAgreement()
        {

            StartDate = new DateTimeOffset(DateTime.Now),
            EndDate = new DateTimeOffset(DateTime.Now.AddDays(1)),
            ReceiverTin = "12345678"
        };

        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        response.EnsureSuccessStatusCode();
    }
}
