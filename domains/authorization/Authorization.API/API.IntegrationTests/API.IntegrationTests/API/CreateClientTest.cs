using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ClientType = API.Authorization.Controllers.ClientType;

namespace API.IntegrationTests.API;

public class CreateClientTest : IntegrationTestBase, IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly Api _api;

    public CreateClientTest(IntegrationTestFixture fixture) : base(fixture)
    {
        _api = _fixture.WebAppFactory.CreateApi(sub: _fixture.WebAppFactory.IssuerIdpClientId.ToString());
    }

    [Fact]
    public async Task GivenValidInternalClient_WhenCreatingClient_ThenReturn201CreatedClientResponse()
    {
        Guid idpClientId = Guid.NewGuid();

        var response = await _api.CreateClient(idpClientId, "Test Client", ClientType.Internal, "http://localhost:5000");
        var client = await response.Content.ReadFromJsonAsync<CreateClientResponse>(_api.SerializerOptions);
        var dbClient = await _fixture.DbContext.Clients.FirstOrDefaultAsync(x => x.IdpClientId == new IdpClientId(idpClientId));

        response.Should().Be201Created();
        dbClient!.IdpClientId.Value.Should().Be(idpClientId);
        client!.Id.Should().Be(dbClient.Id);
        client.IdpClientId.Should().Be(dbClient.IdpClientId.Value);
        client.Name.Should().Be(dbClient.Name.Value);
        client.RedirectUrl.Should().Be(dbClient.RedirectUrl);
    }

    [Fact]
    public async Task GivenValidExternalClient_WhenCreatingClient_ThenReturn201CreatedClientResponse()
    {
        Guid idpClientId = Guid.NewGuid();

        var response = await _api.CreateClient(idpClientId, "Test Client", ClientType.External, "http://localhost:5000");
        var client = await response.Content.ReadFromJsonAsync<CreateClientResponse>(_api.SerializerOptions);
        var dbClient = await _fixture.DbContext.Clients.FirstOrDefaultAsync(x => x.IdpClientId == new IdpClientId(idpClientId));
        var dbOrganization = await _fixture.DbContext.Organizations.FirstOrDefaultAsync(o => o.Id == dbClient!.OrganizationId);

        response.Should().Be201Created();
        client!.Id.Should().Be(dbClient!.Id);
        client.IdpClientId.Should().Be(dbClient.IdpClientId.Value);
        client.Name.Should().Be(dbClient.Name.Value);
        client.RedirectUrl.Should().Be(dbClient.RedirectUrl);

        dbOrganization.Should().NotBeNull();
        dbOrganization!.Id.Should().Be(dbClient.OrganizationId!.Value);
        dbOrganization.Name.Value.Should().Be(client.Name);
    }
}
