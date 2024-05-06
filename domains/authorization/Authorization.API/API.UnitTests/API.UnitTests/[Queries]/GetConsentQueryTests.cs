using API.Authorization._Features_;
using API.Repository;
using API.UnitTests.Repository;
using FluentAssertions;

namespace API.UnitTests._Queries_;

public class GetConsentQueryTests
{
    [Fact]
    async Task GivenClientId_WhenGettingConsent_ConsentReturned()
    {
        var consentRepository = new FakeConsentRepository();
        var consent = Any.Consent();
        await consentRepository.AddAsync(consent, CancellationToken.None);
        var handler = new GetConsentQueryHandler(consentRepository);

        var result = await handler.Handle(new GetConsentQuery(consent.ClientId), CancellationToken.None);

        result.ClientId.Should().Be(consent.ClientId);
        result.Name.Should().Be(consent.Organization.OrganizationName);
        result.RedirectUrl.Should().Be(consent.Client.RedirectUrl);
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGettingConsent_ExceptionIsThrown()
    {
        var consentRepository = new FakeConsentRepository();
        var handler = new GetConsentQueryHandler(consentRepository);

        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await handler.Handle(new GetConsentQuery(Any.IdpClientId().Value), CancellationToken.None));
    }
}
