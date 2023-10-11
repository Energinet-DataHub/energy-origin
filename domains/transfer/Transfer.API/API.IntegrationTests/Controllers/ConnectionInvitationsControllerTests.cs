using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Models;
using Newtonsoft.Json;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class ConnectionInvitationsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly string sub;
    private readonly string tin;

    public ConnectionInvitationsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid().ToString();
        tin = "12345678";
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
        var senderClient = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var createResponse = await senderClient.PostAsync("api/connection-invitations", new StringContent("", Encoding.UTF8, "application/json"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "36923692");

        var getResponse = await receiverClient.GetAsync($"api/connection-invitations/{createdInvitation.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponseBody = await getResponse.Content.ReadAsStringAsync();
        var returnedInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(getResponseBody);

        returnedInvitation.Should().BeEquivalentTo(createdInvitation);
    }

    [Fact]
    public async Task GetConnectionInvitation_ShouldReturnBadRequest_WhenCurrentUserIsSender()
    {
        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var createResponse = await client.PostAsync("api/connection-invitations", new StringContent("", Encoding.UTF8, "application/json"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(createResponseBody);

        var response = await client.GetAsync($"api/connection-invitations/{createdInvitation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();

        responseBody.Should().Be("You cannot Accept/Deny your own ConnectionInvitation");
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
    public async Task GetConnectionInvitation_ShouldReturnConflict_WhenConnectionExists()
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
            SenderCompanyTin = companyATin,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await factory.SeedConnections(new List<Connection> { connection });
        await factory.SeedConnectionInvitations(new List<ConnectionInvitation> { invitation });

        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/connection-invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("Company is already a connection");
    }

    [Fact]
    public async Task GetConnectionInvitation_ShouldReturnNotFound_WhenConnectionInvitationExpired()
    {
        var companyId = Guid.NewGuid();
        var companyTin = "12345678";
        var invitationId = Guid.NewGuid();

        var invitation = new ConnectionInvitation
        {
            Id = invitationId,
            SenderCompanyId = companyId,
            SenderCompanyTin = companyTin,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14)
        };

        await factory.SeedConnectionInvitations(new List<ConnectionInvitation> { invitation });

        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/connection-invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("Connection-invitation expired or deleted");
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_OnSuccessfulDelete()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var postResponse = await authenticatedClient.PostAsync("api/connection-invitations", null);

        var createdInvitation = await postResponse.Content.ReadFromJsonAsync<ConnectionInvitation>();
        var createdId = createdInvitation!.Id;

        var deleteResponse = await authenticatedClient.DeleteAsync($"api/connection-invitations/{createdId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenConnectionInvitationNonExisting()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var randomGuid = Guid.NewGuid();

        var deleteResponse = await authenticatedClient.DeleteAsync($"api/connection-invitations/{randomGuid}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
