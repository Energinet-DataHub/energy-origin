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
}
