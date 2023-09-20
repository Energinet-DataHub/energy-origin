using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Dtos;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class CvrControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>,
    IClassFixture<CvrWireMock>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly CvrWireMock dataSyncWireMock;

    public CvrControllerTests(TransferAgreementsApiWebApplicationFactory factory,
        CvrWireMock dataSyncWireMock)
    {
        this.factory = factory;
        this.dataSyncWireMock = dataSyncWireMock;
        factory.WalletUrl = "UnusedWalletUrl";
        factory.CvrBaseUrl = dataSyncWireMock.Url;
    }

    [Fact]
    public async Task GetCvrCompany_WhenCorrectCvrNumber_ShouldReturnCvrInfo()
    {
        var cvrNumber = "10150817";
        dataSyncWireMock.SetupCvrResponse();

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();
        response.CompanyCvr.Should().Be(cvrNumber);
    }

    [Fact]
    public async Task GetCvrCompany_WhenWrongCvrNumber_ShouldReturnNotFound()
    {
        var cvrNumber = "123";
        dataSyncWireMock.SetupEmptyCvrResponse();
        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetAsync($"api/cvr/{cvrNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCvrCompany_WhenTransientError_ShouldRetry()
    {
        var cvrNumber = "10150817";
        dataSyncWireMock.SetupUnstableServer();

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();
        response.CompanyCvr.Should().Be(cvrNumber);
    }
}
