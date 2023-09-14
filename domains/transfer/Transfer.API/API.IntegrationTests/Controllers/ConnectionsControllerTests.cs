using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.IntegrationTests.Factories;
using API.Models;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class ConnectionsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public ConnectionsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.WalletUrl = "UnusedWalletUrl";
    }

    [Fact]
    public async void GetConnections_ShouldOnlyReturnConnectionsInvolvingTheCompany()
    {
        var myCompanyId = Guid.NewGuid();
        var myCompanyTin = "12345678";
        var ownedConnection1 = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = myCompanyId,
            CompanyATin = myCompanyTin,
            CompanyBId = Guid.NewGuid(),
            CompanyBTin = "12345679"
        };
        var ownedConnection2 = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = Guid.NewGuid(),
            CompanyATin = "23456789",
            CompanyBId = myCompanyId,
            CompanyBTin = myCompanyTin
        };
        var notOwnedConnection = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = Guid.NewGuid(),
            CompanyATin = "34567891",
            CompanyBId = Guid.NewGuid(),
            CompanyBTin = "45679012"
        };

        await factory.SeedConnections(new List<Connection>
        {
            ownedConnection1,
            ownedConnection2,
            notOwnedConnection
        });

        var client = factory.CreateAuthenticatedClient(sub: myCompanyId.ToString());

        var response = await client.GetFromJsonAsync<ConnectionsResponse>("api/connections");

        response.Should().NotBeNull();
        response.Result.Should().HaveCount(2);
        response.Result.Any(x => x.CompanyTin == ownedConnection1.CompanyBTin).Should().BeTrue();
        response.Result.Any(x => x.CompanyTin == ownedConnection2.CompanyATin).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConnection_ShouldReturnNotFound_WhenConnectionDoesNotExist()
    {
        var nonExistentConnectionId = Guid.NewGuid();
        var myCompanyId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(sub: myCompanyId.ToString());

        var response = await client.DeleteAsync($"api/connections/{nonExistentConnectionId}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConnection_ShouldReturnNotFound_WhenUserIsUnauthorized()
    {
        var myCompanyId = Guid.NewGuid();
        var myCompanyTin = "12345678";
        var unauthorizedCompanyId = Guid.NewGuid();

        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = myCompanyId,
            CompanyATin = myCompanyTin,
            CompanyBId = Guid.NewGuid(),
            CompanyBTin = "87654321"
        };

        await factory.SeedConnections(new List<Connection> { connection });

        var unauthorizedClient = factory.CreateAuthenticatedClient(sub: unauthorizedCompanyId.ToString());

        var response = await unauthorizedClient.DeleteAsync($"api/connections/{connection.Id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_AndReduceConnectionCount_WhenConnectionIsDeletedSuccessfully()
    {
        var myCompanyId = Guid.NewGuid();
        var myCompanyTin = "12345678";
        var connectionToBeDeleted = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = myCompanyId,
            CompanyATin = myCompanyTin,
            CompanyBId = Guid.NewGuid(),
            CompanyBTin = "12345679"
        };
        var anotherConnection = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = myCompanyId,
            CompanyATin = myCompanyTin,
            CompanyBId = Guid.NewGuid(),
            CompanyBTin = "12345680"
        };

        await factory.SeedConnections(new List<Connection> { connectionToBeDeleted, anotherConnection });
        var client = factory.CreateAuthenticatedClient(sub: myCompanyId.ToString());

        var deleteResponse = await client.DeleteAsync($"api/connections/{connectionToBeDeleted.Id}");

        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var finalGetResponse = await client.GetFromJsonAsync<ConnectionsResponse>("api/connections");
        finalGetResponse?.Result.Should().HaveCount(1);
    }
}
