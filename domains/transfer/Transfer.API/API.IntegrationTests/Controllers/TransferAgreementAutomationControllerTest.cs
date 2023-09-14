using System;
using System.Net.Http;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.Controllers;
using API.IntegrationTests.Factories;
using API.TransferAgreementsAutomation;
using Argon;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class TransferAgreementAutomationControllerTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly HttpClient authenticatedClient;

    public TransferAgreementAutomationControllerTest(TransferAgreementsApiWebApplicationFactory factory)
    {
        authenticatedClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOk()
    {
        StatusCache cache = new();
        cache.Cache.Clear();

        var response = await authenticatedClient.GetAsync("transfer-automation/status");
        response.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<TransferAutomationStatus>(await response.Content.ReadAsStringAsync());

        status.Should().BeEquivalentTo(new TransferAutomationStatus(CacheValues.Error));

    }
}
