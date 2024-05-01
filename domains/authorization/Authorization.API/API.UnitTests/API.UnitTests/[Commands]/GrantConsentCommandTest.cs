using API.Authorization._Features_;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using Microsoft.EntityFrameworkCore;

namespace API.UnitTests._Commands_;

public class GrantConsentCommandTest
{
    private readonly FakeClientRepository _fakeClientRepository = new();
    private readonly FakeUserRepository _fakeUserRepository = new();
    private readonly FakeOrganizationRepository _fakeOrganizationRepository = new();
    private readonly FakeConsentRepository _fakeConsentRepository = new();
    private readonly FakeAffiliationRepository _fakeAffiliationRepository = new();
    private readonly FakeUnitOfWork _fakeUnitOfWork = new();

    [Fact]
    public async Task GivenUserAndClient_WhenGrantingConsent_ConsentIsCreated()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var client = Any.Client();
        await _fakeAffiliationRepository.AddAsync(Affiliation.Create(user, organization), CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeClientRepository.AddAsync(client, CancellationToken.None);

        var command = new GrantConsentCommand(user.Id, organization.Id, client.Id);
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _fakeConsentRepository,
            _fakeAffiliationRepository, _fakeUnitOfWork);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(await _fakeConsentRepository.Query().ToListAsync());
        Assert.True(_fakeUnitOfWork.Committed);
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGrantingConsent_ExceptionIsThrown()
    {
        var user = Any.User();
        var organization = Any.Organization();
        await _fakeAffiliationRepository.AddAsync(Affiliation.Create(user, organization), CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);

        var command = new GrantConsentCommand(user.Id, organization.Id, Any.Guid());
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _fakeConsentRepository,
            _fakeAffiliationRepository, _fakeUnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GivenUnknownOrganizationId_WhenGrantingConsent_ExceptionIsThrown()
    {
        var client = Any.Client();
        await _fakeClientRepository.AddAsync(client, CancellationToken.None);

        var command = new GrantConsentCommand(Any.Guid(), Any.Guid(), client.Id);
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _fakeConsentRepository,
            _fakeAffiliationRepository, _fakeUnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GivenUserNotAffiliatedWithOrganization_WhenGrantingConsent_ExceptionIsThrown()
    {
        var client = Any.Client();
        var user = Any.User();
        var organization = Any.Organization();

        await _fakeClientRepository.AddAsync(client, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);

        var command = new GrantConsentCommand(user.Id, organization.Id, client.Id);
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _fakeConsentRepository,
            _fakeAffiliationRepository, _fakeUnitOfWork);

        await Assert.ThrowsAsync<UserNotAffiliatedWithOrganizationCommandException>(async () => await handler.Handle(command, CancellationToken.None));
    }
}
