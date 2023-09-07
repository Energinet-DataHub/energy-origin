using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
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
    public async void GetConnections_ShouldOnlyReturnConnectionsInvolvingTheCompany()
    {
        var myCompanyId = Guid.NewGuid();
        var ownedConnection1 = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = myCompanyId,
            CompanyATin = "12345678",
            CompanyBId = Guid.NewGuid(),
            CompanyBTin = "12345679"
        };
        var ownedConnection2 = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = Guid.NewGuid(),
            CompanyATin = "23456789",
            CompanyBId = myCompanyId,
            CompanyBTin = "12345678"
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

        var response = await client.GetFromJsonAsync<ConnectionsResponse>($"api/connections");

        response.Should().NotBeNull();
        response.Result.Should().HaveCount(2);
        response.Result.Any(x => x.ComnpanyTin == ownedConnection1.CompanyBTin).Should().BeTrue();
        response.Result.Any(x => x.ComnpanyTin == ownedConnection2.CompanyATin).Should().BeTrue();
    }
}
