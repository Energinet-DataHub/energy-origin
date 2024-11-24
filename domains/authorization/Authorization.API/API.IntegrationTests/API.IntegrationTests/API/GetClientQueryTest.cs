using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.UnitTests;
using FluentAssertions;

namespace API.IntegrationTests.API;

public class GetClientQueryTest : IntegrationTestBase, IAsyncLifetime
{
    private readonly Api _api;

    public GetClientQueryTest(IntegrationTestFixture fixture) : base(fixture)
    {
        _api = _fixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenIdpClientId_WhenGettingClient_ClientReturned()
    {
        var client = Any.Client();
        await _fixture.DbContext.Clients.AddAsync(client);
        await _fixture.DbContext.SaveChangesAsync();
        var response = await _api.GetClient(client.IdpClientId.Value);
        response.Should().Be200Ok();
        var content = await response.Content.ReadFromJsonAsync<ClientResponse>();
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
