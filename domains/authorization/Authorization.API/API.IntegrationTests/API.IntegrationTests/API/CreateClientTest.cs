using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ClientType = API.Authorization.Controllers.ClientType;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class CreateClientTest
{
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public CreateClientTest(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        _api = integrationTestFixture.WebAppFactory.CreateApi(sub: _integrationTestFixture.WebAppFactory.IssuerIdpClientId.ToString());
    }

    [Fact]
    public async Task Given_When_Then()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(_options);
        Guid idpClientId = Guid.NewGuid();

        // Act
        var response = await _api.CreateClient(idpClientId, "Test Client", ClientType.Internal, "http://localhost:5000");
        var client = await response.Content.ReadFromJsonAsync<CreateClientResponse>(_api.SerializerOptions);
        var dbClient = await dbContext.Clients.FirstOrDefaultAsync(x => x.IdpClientId == new IdpClientId(idpClientId));

        // Assert
        response.Should().Be201Created();
        dbClient!.IdpClientId.Value.Should().Be(idpClientId);
        client!.Id.Should().Be(dbClient.Id);
    }
}

