using System;
using System.Collections.Generic;
using System.Linq;
using API.ApiModels.Responses;
using API.IntegrationTests.Factories;
using API.Models;
using FluentAssertions;
using Newtonsoft.Json;
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
    public async void GetConnections_ShouldOnlyReturnOwnedConnections()
    {
        var sub = Guid.NewGuid();
        var ownedConnection = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            CompanyTin = "12345678",
            OwnerId = sub
        };
        var notOwnedConnection = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            CompanyTin = "12345679",
            OwnerId = Guid.NewGuid()
        };

        await factory.SeedConnections(new List<Connection>
        {
            ownedConnection,
            notOwnedConnection
        });

        var client = factory.CreateAuthenticatedClient(sub: sub.ToString());

        var get = await client.GetAsync($"api/connections");
        get.EnsureSuccessStatusCode();
        var response = JsonConvert.DeserializeObject<ConnectionsResponse>(await get.Content.ReadAsStringAsync());

        response.Should().NotBeNull();
        response.Result.Should().HaveCount(1);
        response.Result.FirstOrDefault().CompanyTin.Should().Be(ownedConnection.CompanyTin);
    }
}
