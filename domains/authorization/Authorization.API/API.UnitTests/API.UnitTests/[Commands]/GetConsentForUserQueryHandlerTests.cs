using API.Authorization._Features_;
using API.Data;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Commands_;

public class GetConsentForUserQueryHandlerTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly FakeUserRepository _fakeUserRepository;
    private readonly FakeTermsRepository _fakeTermsRepository;
    private readonly IUnitOfWork _fakeUnitOfWork;
    private readonly GetConsentForUserQueryHandler _handler;

    public GetConsentForUserQueryHandlerTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _fakeUserRepository = new FakeUserRepository();
        _fakeTermsRepository = new FakeTermsRepository();
        _fakeUnitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new GetConsentForUserQueryHandler(
            _fakeOrganizationRepository,
            _fakeUserRepository,
            _fakeTermsRepository,
            _fakeUnitOfWork);
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_ReturnsResultWithNoOrgIdsAndFalseTermsAccepted()
    {
        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");
        await _fakeTermsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeEquivalentTo(new GetConsentForUserCommandResult(
            command.Sub,
            command.Name,
            "User",
            command.OrgName,
            new List<Guid>(),
            "dashboard production meters certificates wallet",
            false
        ));
        await _fakeUnitOfWork.Received(1).RollbackAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_ReturnsResultWithOrgIdsAndFalseTermsAccepted()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Test User"));
        var affiliation = Affiliation.Create(user, organization);
        organization.Affiliations.Add(affiliation);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeEquivalentTo(new GetConsentForUserCommandResult(
            command.Sub,
            command.Name,
            "User",
            command.OrgName,
            new List<Guid> { organization.Id },
            "dashboard production meters certificates wallet",
            false
        ));
        await _fakeUnitOfWork.Received(1).RollbackAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsAndTermsAcceptedButUserDoesNotExist_CreatesUserAndAffiliation()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var terms = Terms.Create("1.0");
        organization.AcceptTerms(terms);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(terms, CancellationToken.None);

        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgIds.Should().ContainSingle().Which.Should().Be(organization.Id);
        result.TermsAccepted.Should().BeTrue();
        _fakeUserRepository.Query().Count().Should().Be(1);
        organization.Affiliations.Any(a => a.User.IdpUserId.Value == command.Sub).Should().BeTrue();
        await _fakeUnitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsAndTermsAcceptedAndUserExists_ReturnsResultWithoutCreatingNewEntities()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var terms = Terms.Create("1.0");
        organization.AcceptTerms(terms);
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Test User"));
        Affiliation.Create(user, organization);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(terms, CancellationToken.None);

        var command = new GetConsentForUserCommand(user.IdpUserId.Value, "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgIds.Should().ContainSingle().Which.Should().Be(organization.Id);
        result.TermsAccepted.Should().BeTrue();
        _fakeUserRepository.Query().Count().Should().Be(1);
        organization.Affiliations.Count.Should().Be(1);
        await _fakeUnitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndThrowsException()
    {
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(x => throw new Exception("Test exception"));
        var handler = new GetConsentForUserQueryHandler(
            mockOrganizationRepository,
            _fakeUserRepository,
            _fakeTermsRepository,
            _fakeUnitOfWork);

        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await _fakeUnitOfWork.Received(1).RollbackAsync();
    }
}
