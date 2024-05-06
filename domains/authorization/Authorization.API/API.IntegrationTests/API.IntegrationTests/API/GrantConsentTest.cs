using System.Net.Http.Json;
using API.Authorization._Features_;
using API.IntegrationTests.Setup;
using FluentAssertions;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GrantConsentTest
{
    private readonly Api _api;

    public GrantConsentTest(IntegrationTestFixture integrationTestFixture)
    {
        _api = integrationTestFixture.Api;
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGrantingConsent_HttpNotFoundIsReturned()
    {
        var unknownClientId = Guid.NewGuid();
        var response = await _api.GrantConsent(unknownClientId);
        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenClientId_WhenGettingConsent_HttpOkConsentReturned()
    {
        var clientId = Guid.NewGuid();
        var response = await _api.GetConsent(clientId);
        response.Should().Be200Ok();
        var result =  await response.Content.ReadFromJsonAsync<GetConsentQueryResult>();
        result!.ClientId.Should().Be(clientId);
    }
}
