using System.Net.Http.Json;
using API.Authorization._Features_;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetClientQueryTest
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> options;

    public GetClientQueryTest(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.ConnectionString;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.CreateApi();
    }

    [Fact]
    public async Task GivenIdpClientId_WhenGettingClient_ClientReturned()
    {

        var client = Any.Client();
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Clients.AddAsync(client);
        await dbContext.SaveChangesAsync();

        var api = _integrationTestFixture.CreateApi();
        var response = await api.GetClient(client.IdpClientId.Value);
        response.Should().Be200Ok();

        var content = await response.Content.ReadFromJsonAsync<ClientResponse>();

        content!.IdpClientId.Should().Be(client.IdpClientId.Value);
        content.Name.Should().Be(client.Name.Value);
        content.RedirectUrl.Should().Be(client.RedirectUrl);
    }

    [Fact]
    public async Task GivenUnknownIdpClientId_WhenGettingClient_404NotFound()
    {

        var api = _integrationTestFixture.CreateApi();
        var response = await api.GetClient(Guid.NewGuid());
        response.Should().Be404NotFound();
    }
}
