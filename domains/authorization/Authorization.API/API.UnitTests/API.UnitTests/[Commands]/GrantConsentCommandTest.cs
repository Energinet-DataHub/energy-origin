using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
/*
namespace API.UnitTests._Commands_;

public class GrantConsentCommandTest
{
    private readonly FakeClientRepository _fakeClientRepository = new();
    private readonly FakeUserRepository _fakeUserRepository = new();
    private readonly FakeOrganizationRepository _fakeOrganizationRepository = new();
    private readonly FakeUnitOfWork _fakeUnitOfWork = new();
    private readonly FakeUserRepository _userRepository = new();

    [Fact]
    public async Task GivenUserAndClient_WhenGrantingConsent_ConsentIsCreated()
    {
        var user = Any.User();
        var organization = Any.Organization();
        Affiliation.Create(user, organization);
        var client = Any.Client();
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeClientRepository.AddAsync(client, CancellationToken.None);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var command = new GrantConsentCommand(user.IdpUserId.Value, organization.Tin!.Value, new IdpClientId(client.IdpClientId.Value));
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _userRepository, _fakeUnitOfWork);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(_fakeClientRepository.Query().First().Consents);
        Assert.Single(_fakeOrganizationRepository.Query().First().Consents);
        Assert.True(_fakeUnitOfWork.Committed);
    }

    [Fact]
    public async Task GivenUserAndClient_WhenGrantingConsentMultipleTimes_ConsentIsCreatedOnce()
    {
        var user = Any.User();
        var organization = Any.Organization();
        Affiliation.Create(user, organization);
        var client = Any.Client();
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeClientRepository.AddAsync(client, CancellationToken.None);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var command = new GrantConsentCommand(user.IdpUserId.Value, organization.Tin!.Value, new IdpClientId(client.IdpClientId.Value));
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _userRepository, _fakeUnitOfWork);
        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(_fakeClientRepository.Query().First().Consents);
        Assert.Single(_fakeOrganizationRepository.Query().First().Consents);
        Assert.True(_fakeUnitOfWork.Committed);
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGrantingConsent_ExceptionIsThrown()
    {
        var user = Any.User();
        var organization = Any.Organization();
        Affiliation.Create(user, organization);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var command = new GrantConsentCommand(user.Id, organization.Tin!.Value, Any.IdpClientId());
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _userRepository, _fakeUnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GivenUnknownOrganizationId_WhenGrantingConsent_ExceptionIsThrown()
    {
        var user = Any.User();
        var client = Any.Client();
        await _fakeClientRepository.AddAsync(client, CancellationToken.None);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var command = new GrantConsentCommand(Any.Guid(), "66776677", new IdpClientId(client.IdpClientId.Value));
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _userRepository, _fakeUnitOfWork);

        await Assert.ThrowsAsync<UserNotAffiliatedWithOrganizationCommandException>(async () =>
            await handler.Handle(command, CancellationToken.None));
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

        var command = new GrantConsentCommand(user.Id, organization.Tin!.Value, new IdpClientId(client.IdpClientId.Value));
        var handler = new GrantConsentCommandHandler(_fakeClientRepository, _fakeOrganizationRepository, _userRepository, _fakeUnitOfWork);

        await Assert.ThrowsAsync<UserNotAffiliatedWithOrganizationCommandException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }
} TODO
*/
