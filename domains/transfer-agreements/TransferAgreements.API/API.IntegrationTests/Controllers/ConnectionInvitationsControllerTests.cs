using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Models;
using Argon;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Controllers;

[UsesVerify]
public class ConnectionInvitationsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly string sub;
    private readonly string tin = "12345678";

    public ConnectionInvitationsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid().ToString();

        factory.WalletUrl = "UnusedWalletUrl";
    }

    [Fact]
    public async Task Create_ShouldReturnInvitation_WhenInvitationIsCreated()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var result = await authenticatedClient
            .PostAsync("api/connection-invitations", null);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateConnectionInvitation_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.PostAsync("api/connection-invitations", null);

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConnectionInvitation_ShouldReturnOK_WhenInvitationExists()
    {
        var invitationId = Guid.NewGuid();
        var invitation = new ConnectionInvitation
        {
            Id = invitationId,
            SenderCompanyId = Guid.NewGuid(),
            SenderCompanyTin = "12345678"
        };

        await factory.SeedConnectionInvitations(new List<ConnectionInvitation> { invitation });

        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/connection-invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var returnedInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(responseBody);

        returnedInvitation.Should().BeEquivalentTo(invitation);
    }


    [Fact]
    public async Task GetConnectionInvitation_ShouldReturnNotFound_WhenInvitationDoesNotExist()
    {
        var nonExistentInvitationId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/connection-invitations/{nonExistentInvitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("Connection-invitation expired or deleted");
    }

    [Fact]
    public async Task GetConnectionInvitation_ShouldReturnConflict_WhenConflictExists()
    {
        var companyAId = Guid.NewGuid();
        var companyATin = "12345678";
        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = companyAId,
            CompanyATin = companyATin,
            CompanyBId = Guid.Parse(sub),
            CompanyBTin = tin
        };

        var invitationId = Guid.NewGuid();
        var invitation = new ConnectionInvitation
        {
            Id = invitationId,
            SenderCompanyId = companyAId,
            SenderCompanyTin = companyATin
        };

        await factory.SeedConnections(new List<Connection> { connection });
        await factory.SeedConnectionInvitations(new List<ConnectionInvitation> { invitation });

        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/connection-invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("Company is already a connection");
    }
}
