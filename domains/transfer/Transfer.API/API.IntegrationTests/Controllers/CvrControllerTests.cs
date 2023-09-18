using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Dtos;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class CvrControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public CvrControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.WalletUrl = "UnusedWalletUrl";
    }

    [Fact]
    public async Task GetCvrCompany_WhenCorrectCvrNumber_ShouldReturnCvrInfo()
    {
        var cvrNumber = "10150817";
        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();
        response.CompanyCvr.Should().Be(cvrNumber);
    }

    [Fact]
    public async Task GetCvrCompany_WhenWrongCvrNumber_ShouldReturnNotFound()
    {
        var cvrNumber = "123";
        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetAsync($"api/cvr/{cvrNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
