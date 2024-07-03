using API.Authorization._Features_;
using API.Data;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Commands_;

public class AcceptTermsCommandHandlerTests
{
    private readonly FakeOrganizationRepository _organizationRepository;
    private readonly FakeUserRepository _userRepository;
    private readonly FakeTermsRepository _termsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AcceptTermsCommandHandler _handler;

    public AcceptTermsCommandHandlerTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _userRepository = new FakeUserRepository();
        _termsRepository = new FakeTermsRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AcceptTermsCommandHandler(_organizationRepository, _userRepository, _termsRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_CreatesNewOrganization()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        await _termsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _organizationRepository.Query().Count().Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_UpdatesTerms()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var organization = Organization.Create(new Tin(command.OrgCvr), new OrganizationName("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be("1.0");
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenNoTermsExist_ThrowsInvalidOperationException()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        await _unitOfWork.Received(1).RollbackAsync();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CreatesNewUserAndAffiliation()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var organization = Organization.Create(new Tin(command.OrgCvr), new OrganizationName("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _userRepository.Query().Count().Should().Be(1);
        organization.Affiliations.Any(a => a.User.IdpUserId.Value == command.UserId).Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenUserExists_CreatesNewAffiliation()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var organization = Organization.Create(new Tin(command.OrgCvr), new OrganizationName("Test Org"));
        var user = User.Create(IdpUserId.Create(command.UserId), UserName.Create(command.UserName));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _userRepository.AddAsync(user, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        organization.Affiliations.Any(a => a.User.IdpUserId.Value == command.UserId).Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransaction()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(x => throw new Exception("Test exception"));
        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptTermsCommandHandler(mockOrganizationRepository, _userRepository, _termsRepository, mockUnitOfWork);

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await mockUnitOfWork.Received(1).RollbackAsync();
    }
}
