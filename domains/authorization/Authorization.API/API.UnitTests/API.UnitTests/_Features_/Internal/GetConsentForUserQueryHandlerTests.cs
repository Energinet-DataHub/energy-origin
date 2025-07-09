using API.Authorization._Features_.Internal;
using API.Data;
using API.Metrics;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Features_.Internal;

public class GetConsentForUserQueryHandlerTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly FakeUserRepository _fakeUserRepository;
    private readonly FakeTermsRepository _fakeTermsRepository;
    private readonly IAuthorizationMetrics _fakeMetrics;
    private readonly IUnitOfWork _fakeUnitOfWork;
    private readonly GetConsentForUserQueryHandler _handler;

    public GetConsentForUserQueryHandlerTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _fakeUserRepository = new FakeUserRepository();
        _fakeTermsRepository = new FakeTermsRepository();
        _fakeUnitOfWork = Substitute.For<IUnitOfWork>();
        _fakeMetrics = Substitute.For<IAuthorizationMetrics>();
        _handler = new GetConsentForUserQueryHandler(
            _fakeOrganizationRepository,
            _fakeUserRepository,
            _fakeTermsRepository,
            _fakeUnitOfWork,
            _fakeMetrics);
    }

    private const string Scope = "dashboard production meters certificates wallet";
    private const string SubType = "User";

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_ReturnsResultWithNoOrgIdsAndFalseTermsAccepted()
    {
        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");
        await _fakeTermsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeEquivalentTo(new GetConsentForUserCommandResult(
            command.Sub,
            command.Name,
            SubType,
            command.OrgName,
            Guid.Empty,
            new List<Guid>(),
            Scope,
            false
        ));
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_ReturnsResultWithOrgIdsAndFalseTermsAccepted()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Test User"));
        var affiliation = Affiliation.Create(user, organization);
        organization.Affiliations.Add(affiliation);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeEquivalentTo(new GetConsentForUserCommandResult(
            command.Sub,
            command.Name,
            SubType,
            command.OrgName,
            organization.Id,
            new List<Guid>(),
            Scope,
            false
        ));
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsAndTermsAcceptedButUserDoesNotExist_CreatesUserAndAffiliation()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var terms = Terms.Create(1);
        organization.AcceptTerms(terms, true);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(terms, CancellationToken.None);

        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgId.Should().Be(organization.Id);
        result.TermsAccepted.Should().BeTrue();
        _fakeUserRepository.Query().Count().Should().Be(1);
        organization.Affiliations.Any(a => a.User.IdpUserId.Value == command.Sub).Should().BeTrue();
        await _fakeUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsAndTermsAcceptedAndUserExists_ReturnsResultWithoutCreatingNewEntities()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var terms = Terms.Create(1);
        organization.AcceptTerms(terms, true);
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Test User"));
        Affiliation.Create(user, organization);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(terms, CancellationToken.None);

        var command = new GetConsentForUserCommand(user.IdpUserId.Value, "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgId.Should().Be(organization.Id);
        result.TermsAccepted.Should().BeTrue();
        _fakeUserRepository.Query().Count().Should().Be(1);
        organization.Affiliations.Count.Should().Be(1);
        await _fakeUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLatestTermsVersionIsHigherThanAcceptedTerms_ReturnsResultWithFalseTermsAccepted()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        var oldTerms = Terms.Create(1);
        organization.AcceptTerms(oldTerms, true);
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Test User"));
        Affiliation.Create(user, organization);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(oldTerms, CancellationToken.None);

        var newTerms = Terms.Create(2);
        await _fakeTermsRepository.AddAsync(newTerms, CancellationToken.None);

        var command = new GetConsentForUserCommand(user.IdpUserId.Value, "Test User", "Test Org", "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgId.Should().Be(organization.Id);
        result.TermsAccepted.Should().BeFalse();
        _fakeUserRepository.Query().Count().Should().Be(1);
        organization.Affiliations.Count.Should().Be(1);
        organization.TermsVersion.Should().Be(oldTerms.Version);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndThrowsException()
    {
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(_ => throw new Exception("Test exception"));
        var handler = new GetConsentForUserQueryHandler(
            mockOrganizationRepository,
            _fakeUserRepository,
            _fakeTermsRepository,
            _fakeUnitOfWork,
            _fakeMetrics);

        var command = new GetConsentForUserCommand(Guid.NewGuid(), "Test User", "Test Org", "12345678");

        await _fakeUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenTrialOrgHasAcceptedLatestTrialTerms_ReturnsTrueTermsAccepted()
    {
        // Arrange
        var trialOrg = Organization.CreateTrial(Tin.Create("87654321"), OrganizationName.Create("Trial Org"));
        var trialTerms = Terms.Create(version: 1, type: TermsType.Trial);
        trialOrg.AcceptTerms(trialTerms, false);

        await _fakeOrganizationRepository.AddAsync(trialOrg, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(trialTerms, CancellationToken.None);

        var cmd = new GetConsentForUserCommand(Guid.NewGuid(), "Trial User", "Trial Org", "87654321");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.OrgId.Should().Be(trialOrg.Id);
        result.TermsAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTrialOrgAcceptedOldTrialTermsAndNewerExist_ReturnsFalseTermsAccepted()
    {
        var trialOrg = Organization.CreateTrial(Tin.Create("87654321"), OrganizationName.Create("Trial Org"));
        var oldTrialTerms = Terms.Create(1, TermsType.Trial);
        trialOrg.AcceptTerms(oldTrialTerms, false);

        await _fakeOrganizationRepository.AddAsync(trialOrg, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(oldTrialTerms, CancellationToken.None);

        // newer trial terms (v2)
        await _fakeTermsRepository.AddAsync(Terms.Create(2, TermsType.Trial), CancellationToken.None);

        var cmd = new GetConsentForUserCommand(Guid.NewGuid(), "Trial User", "Trial Org", "87654321");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.OrgId.Should().Be(trialOrg.Id);
        result.TermsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenLatestTrialTermsVersionIsHigherThanAccepted_ReturnsFalseTermsAccepted()
    {
        // Arrange – trial organisation has accepted v1 of Trial terms
        var trialOrg = Organization.CreateTrial(
            Tin.Create("87654321"),
            OrganizationName.Create("Trial Org"));

        var oldTrialTerms = Terms.Create(1, TermsType.Trial);
        trialOrg.AcceptTerms(oldTrialTerms, isWhitelisted: false);

        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Trial User"));
        Affiliation.Create(user, trialOrg);

        await _fakeOrganizationRepository.AddAsync(trialOrg, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(oldTrialTerms, CancellationToken.None);

        // newer Trial terms version (v2) is now available
        var newTrialTerms = Terms.Create(2, TermsType.Trial);
        await _fakeTermsRepository.AddAsync(newTrialTerms, CancellationToken.None);

        var command = new GetConsentForUserCommand(
            user.IdpUserId.Value,
            "Trial User",
            "Trial Org",
            "87654321");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgId.Should().Be(trialOrg.Id);
        result.TermsAccepted.Should().BeFalse();
        _fakeUserRepository.Query().Count().Should().Be(1);
        trialOrg.Affiliations.Count.Should().Be(1);
        trialOrg.TermsVersion.Should().Be(oldTrialTerms.Version);
    }

    [Fact]
    public async Task Handle_WhenTrialOrgAcceptedLatestTrialTerms_ButNewerNormalTermsExist_ReturnsTrueTermsAccepted()
    {
        var trialOrg = Organization.CreateTrial(
            Tin.Create("87654321"),
            OrganizationName.Create("Trial Org"));

        // latest Trial terms (v1) – organisation has accepted these
        var trialTermsV1 = Terms.Create(1, TermsType.Trial);
        trialOrg.AcceptTerms(trialTermsV1, isWhitelisted: false);

        // persist org, user and accepted Trial terms
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Trial User"));
        Affiliation.Create(user, trialOrg);

        await _fakeOrganizationRepository.AddAsync(trialOrg, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(trialTermsV1, CancellationToken.None);

        // a newer **Normal** terms version appears (v2) – should not influence Trial org terms acceptance
        var normalTermsV2 = Terms.Create(version: 2, type: TermsType.Normal);
        await _fakeTermsRepository.AddAsync(normalTermsV2, CancellationToken.None);

        var command = new GetConsentForUserCommand(
            user.IdpUserId.Value,
            "Trial User",
            "Trial Org",
            "87654321");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgId.Should().Be(trialOrg.Id);
        result.TermsAccepted.Should().BeTrue();
        trialOrg.TermsVersion.Should().Be(trialTermsV1.Version);
        _fakeUserRepository.Query().Count().Should().Be(1);
        trialOrg.Affiliations.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNormalOrgAcceptedLatestNormalTerms_ButNewerTrialTermsExist_ReturnsTrueTermsAccepted()
    {
        var normalOrg = Organization.Create(
            Tin.Create("12345678"),
            OrganizationName.Create("Normal Org"));

        // latest Normal terms (v1) – organisation has accepted these
        var normalTermsV1 = Terms.Create(version: 1, type: TermsType.Normal);
        normalOrg.AcceptTerms(normalTermsV1, isWhitelisted: true);

        // persist org, user and accepted Normal terms
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Normal User"));
        Affiliation.Create(user, normalOrg);

        await _fakeOrganizationRepository.AddAsync(normalOrg, CancellationToken.None);
        await _fakeUserRepository.AddAsync(user, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(normalTermsV1, CancellationToken.None);

        // a newer **Trial** terms version appears (v2) – must not affect Normal org terms acceptance
        var trialTermsV2 = Terms.Create(2, TermsType.Trial);
        await _fakeTermsRepository.AddAsync(trialTermsV2, CancellationToken.None);

        var command = new GetConsentForUserCommand(
            user.IdpUserId.Value,
            "Normal User",
            "Normal Org",
            "12345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OrgId.Should().Be(normalOrg.Id);
        result.TermsAccepted.Should().BeTrue();
        normalOrg.TermsVersion.Should().Be(normalTermsV1.Version);
        _fakeUserRepository.Query().Count().Should().Be(1);
        normalOrg.Affiliations.Count.Should().Be(1);
    }
}
