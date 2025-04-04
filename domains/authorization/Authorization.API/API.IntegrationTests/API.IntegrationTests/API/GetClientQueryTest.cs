using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetClientQueryTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetClientQueryTest(IntegrationTestFixture integrationTestFixture) :base(integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenIdpClientId_WhenGettingClient_ClientReturned()
    {
        var client = Any.Client();
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Clients.AddAsync(client, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var response = await _api.GetClient(client.IdpClientId.Value);
        response.Should().Be200Ok();
        var content = await response.Content.ReadFromJsonAsync<ClientResponse>(TestContext.Current.CancellationToken);
        content!.IdpClientId.Should().Be(client.IdpClientId.Value);
        content.Name.Should().Be(client.Name.Value);
        content.RedirectUrl.Should().Be(client.RedirectUrl);
    }

    [Fact]
    public async Task GivenUnknownIdpClientId_WhenGettingClient_404NotFound()
    {
        var response = await _api.GetClient(Guid.NewGuid());
        response.Should().Be404NotFound();
    }
}
