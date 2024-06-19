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
    private readonly Api _api;
    private readonly Guid _sub;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public CreateClientTest(IntegrationTestFixture integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        _sub = Guid.NewGuid();
        _api = integrationTestFixture.WebAppFactory.CreateApi(_sub.ToString());
    }

    [Fact]
    public async Task Given_When_Then()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(_options);
        Guid idpClientId = Guid.NewGuid();

        // Act
        var response = await _api.CreateClient(idpClientId, "Test Client", ClientType.Internal, "http://localhost:5000");
        var dbClient = await dbContext.Clients.FirstOrDefaultAsync(x => x.IdpClientId == new IdpClientId(idpClientId));

        // Assert
        response.Should().Be200Ok();
        dbClient!.IdpClientId.Value.Should().Be(idpClientId);

    }
}
