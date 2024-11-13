using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_;

public class GrantConsentToOrganizationCommandTest
{
    private readonly FakeOrganizationRepository _organizationRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeOrganizationConsentRepository _organizationConsentRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task GivenTwoOrganizations_WhenGrantingConsent_ConsentIsCreated()
    {
        // Given user and organizations
        var user = Any.User();
        var userOrganization = Any.Organization();
        _ = Affiliation.Create(user, userOrganization);
        var organization = Any.Organization();
        await _userRepository.AddAsync(user, CancellationToken.None);
        await _organizationRepository.AddAsync(userOrganization, CancellationToken.None);
        await _organizationRepository.AddAsync(organization, CancellationToken.None);

        // When handling grant consent command
        var cmd = new GrantConsentToOrganizationCommand(user.IdpUserId.Value, userOrganization.Tin!.Value, OrganizationId.Create(organization.Id));
        var handler = new GrantConsentToOrganizationCommandHandler(_organizationRepository, _userRepository, _organizationConsentRepository,
            _unitOfWork);
        await handler.Handle(cmd, CancellationToken.None);

        // Then consent is created
        _organizationConsentRepository.Query().Count().Should().Be(1);
    }

    [Fact]
    public async Task GivenExistingConsent_WhenGrantingConsent_NoAdditionConsentIsCreated()
    {
        // Given existing consent
        var user = Any.User();
        var userOrganization = Any.Organization();
        _ = Affiliation.Create(user, userOrganization);
        var organization = Any.Organization();
        var existingConsent = OrganizationConsent.Create(userOrganization.Id, organization.Id, DateTimeOffset.Now);
        await _userRepository.AddAsync(user, CancellationToken.None);
        await _organizationRepository.AddAsync(userOrganization, CancellationToken.None);
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _organizationConsentRepository.AddAsync(existingConsent, CancellationToken.None);

        // When handling grant consent command
        var cmd = new GrantConsentToOrganizationCommand(user.IdpUserId.Value, userOrganization.Tin!.Value, OrganizationId.Create(organization.Id));
        var handler = new GrantConsentToOrganizationCommandHandler(_organizationRepository, _userRepository, _organizationConsentRepository,
            _unitOfWork);
        await handler.Handle(cmd, CancellationToken.None);

        // Then no exception is thrown and still one consent exists
        _organizationConsentRepository.Query().Count().Should().Be(1);
    }

    [Fact]
    public async Task GivenOrganization_WhenGrantingConsentToSameOrganization_ExceptionIsThrown()
    {
        // Given user and organization
        var user = Any.User();
        var userOrganization = Any.Organization();
        _ = Affiliation.Create(user, userOrganization);
        await _userRepository.AddAsync(user, CancellationToken.None);
        await _organizationRepository.AddAsync(userOrganization, CancellationToken.None);

        // When granting consent to own organization
        var cmd = new GrantConsentToOrganizationCommand(user.IdpUserId.Value, userOrganization.Tin!.Value, OrganizationId.Create(userOrganization.Id));
        var handler = new GrantConsentToOrganizationCommandHandler(_organizationRepository, _userRepository, _organizationConsentRepository,
            _unitOfWork);

        // Exception is thrown
        await Assert.ThrowsAsync<UnableToGrantConsentToOwnOrganizationException>(async () => await handler.Handle(cmd, CancellationToken.None));
    }
}
