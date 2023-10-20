using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Connections.Api.Dto.Requests;
using API.Connections.Api.Dto.Responses;
using API.Connections.Api.Models;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace API.IntegrationTests.Connections.Api.Controllers;

public class ConnectionsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public ConnectionsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateConnection_ShouldReturnCreated_WhenSuccess()
    {
        var senderCompanyId = Guid.NewGuid();
        var senderClient = factory.CreateAuthenticatedClient(sub: senderCompanyId.ToString());

        var createInvitationRequest = await senderClient.PostAsync("api/connection-invitations", null);
        var createResponseBody = await createInvitationRequest.Content.ReadAsStringAsync();
        var createdInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(createResponseBody);

        var receiverCompanyId = Guid.NewGuid();
        var receiverClient = factory.CreateAuthenticatedClient(sub: receiverCompanyId.ToString());

        var createConnectionResponse = await receiverClient.PostAsJsonAsync("api/connections", new CreateConnection { ConnectionInvitationId = createdInvitation!.Id });

        createConnectionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateConnection_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var senderCompanyId = Guid.NewGuid();
        var senderClient = factory.CreateAuthenticatedClient(sub: senderCompanyId.ToString());

        var createInvitationRequest = await senderClient.PostAsync("api/connection-invitations", null);
        var createResponseBody = await createInvitationRequest.Content.ReadAsStringAsync();
        var createdInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(createResponseBody);

        var unAuthenticatedClient = factory.CreateUnauthenticatedClient();

        var createConnectionResponse = await unAuthenticatedClient.PostAsJsonAsync("api/connections", new CreateConnection { ConnectionInvitationId = createdInvitation!.Id });

        createConnectionResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateConnection_ShouldReturnConflict_WhenConnectionAlreadyExists()
    {
        var senderCompanyId = Guid.NewGuid();
        var senderClient = factory.CreateAuthenticatedClient(sub: senderCompanyId.ToString());

        var createFirstInvitationResponse = await senderClient.PostAsync("api/connection-invitations", null);
        var firstInvitation = await createFirstInvitationResponse.Content.ReadAsStringAsync();
        var firstCreatedInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(firstInvitation);

        var receiverCompanyId = Guid.NewGuid();
        var receiverClient = factory.CreateAuthenticatedClient(sub: receiverCompanyId.ToString());

        await receiverClient.PostAsJsonAsync("api/connections", new CreateConnection { ConnectionInvitationId = firstCreatedInvitation!.Id });

        var createSecondInvitationResponse = await senderClient.PostAsync("api/connection-invitations", null);
        var secondInvitation = await createSecondInvitationResponse.Content.ReadAsStringAsync();
        var secondCreatedInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(secondInvitation);

        var createSecondConnectionResponse = await receiverClient.PostAsJsonAsync("api/connections", new CreateConnection { ConnectionInvitationId = secondCreatedInvitation!.Id });

        createSecondConnectionResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateConnection_ShouldDeleteInvitation_WhenSuccess()
    {
        var senderCompanyId = Guid.NewGuid();
        var senderClient = factory.CreateAuthenticatedClient(sub: senderCompanyId.ToString());

        var createInvitationRequest = await senderClient.PostAsync("api/connection-invitations", null);
        var createResponseBody = await createInvitationRequest.Content.ReadAsStringAsync();
        var createdInvitation = JsonConvert.DeserializeObject<ConnectionInvitation>(createResponseBody);

        var receiverCompanyId = Guid.NewGuid();
        var receiverClient = factory.CreateAuthenticatedClient(sub: receiverCompanyId.ToString());
        await receiverClient.PostAsJsonAsync("api/connections", new CreateConnection { ConnectionInvitationId = createdInvitation!.Id });

        var getInvitationResponse = await senderClient.GetAsync($"api/connection-invitations/{createdInvitation.Id}");

        getInvitationResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseBody = await getInvitationResponse.Content.ReadAsStringAsync();
        responseBody.Should().Be("Connection-invitation expired or deleted");
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
        response!.Result.Should().HaveCount(2);
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
    public async Task DeleteConnection_ShouldReturnNotFound_WhenUserNotOwnerOfConnection()
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
