using API.Authorization._Features_;
using API.Models;
using API.UnitTests.Repository;
using FluentAssertions;
/*
namespace API.UnitTests._Queries_;

public class GetConsentQueryTests
{
    readonly FakeClientRepository _fakeClientRepository = new();

    [Fact]
    async Task GivenClientId_WhenGettingConsent_ConsentReturned()
    {
        var client = Any.Client();
        var organization = Any.Organization();
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);
        await _fakeClientRepository.AddAsync(client, CancellationToken.None);

        var handler = new GetConsentQueryHandler(_fakeClientRepository);
        var result = await handler.Handle(new GetConsentQuery(client.IdpClientId.Value), CancellationToken.None);

        result.Result[0].IdpClientId.Should().Be(client.IdpClientId);
        result.Result[0].OrganizationName.Should().Be(consent.Organization.Name);
        result.Result[0].RedirectUrl.Should().Be(consent.Client.RedirectUrl);
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGettingConsent_EmptyListReturned()
    {
        var handler = new GetConsentQueryHandler(_fakeClientRepository);
        var result = await handler.Handle(new GetConsentQuery(Any.IdpClientId().Value), CancellationToken.None);
        result.Result.Should().BeEmpty();
    }
}
*/
