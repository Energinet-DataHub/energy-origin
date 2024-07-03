using API.Authorization._Features_;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Commands_;

public class AcceptTermsCommandHandlerTests
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITermsRepository _termsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AcceptTermsCommandHandler _handler;

    public AcceptTermsCommandHandlerTests()
    {
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _termsRepository = Substitute.For<ITermsRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AcceptTermsCommandHandler(_organizationRepository, _userRepository, _termsRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_CreatesNewOrganization()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        _organizationRepository.Query().ReturnsForAnyArgs(new List<Organization>().AsQueryable());
        _termsRepository.Query().Returns(new List<Terms> { Terms.Create("1.0") }.AsQueryable());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        await _organizationRepository.Received(1).AddAsync(Arg.Is<Organization>(o => o.Tin.Value == command.OrgCvr), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_UpdatesTerms()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var organization = Organization.Create(new Tin(command.OrgCvr), new OrganizationName("Test Org"));
        _organizationRepository.Query().Returns(new List<Organization> { organization }.AsQueryable());
        _termsRepository.Query().Returns(new List<Terms> { Terms.Create("1.0") }.AsQueryable());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be("1.0");
        _organizationRepository.Received(1).Update(Arg.Is<Organization>(o => o.Tin.Value == command.OrgCvr));
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenNoTermsExist_ThrowsInvalidOperationException()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        _termsRepository.Query().Returns(new List<Terms>().AsQueryable());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        await _unitOfWork.Received(1).RollbackAsync();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CreatesNewUserAndAffiliation()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var organization = Organization.Create(new Tin(command.OrgCvr), new OrganizationName("Test Org"));
        _organizationRepository.Query().Returns(new List<Organization> { organization }.AsQueryable());
        _termsRepository.Query().Returns(new List<Terms> { Terms.Create("1.0") }.AsQueryable());
        _userRepository.Query().ReturnsForAnyArgs(new List<User>().AsQueryable());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.IdpUserId.Value == command.UserId), Arg.Any<CancellationToken>());
        _organizationRepository.Received(1).Update(Arg.Is<Organization>(o => o.Affiliations.Any(a => a.User.IdpUserId.Value == command.UserId)));
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenUserExists_CreatesNewAffiliation()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        var organization = Organization.Create(new Tin(command.OrgCvr), new OrganizationName("Test Org"));
        var user = User.Create(IdpUserId.Create(command.UserId), UserName.Create(command.UserName));
        _organizationRepository.Query().Returns(new List<Organization> { organization }.AsQueryable());
        _termsRepository.Query().Returns(new List<Terms> { Terms.Create("1.0") }.AsQueryable());
        _userRepository.Query().Returns(new List<User> { user }.AsQueryable());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _organizationRepository.Received(1).Update(Arg.Is<Organization>(o => o.Affiliations.Any(a => a.User.IdpUserId.Value == command.UserId)));
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransaction()
    {
        var command = new AcceptTermsCommand("12345678", Guid.NewGuid(), "Test User");
        _organizationRepository.Query().Returns(x => throw new Exception("Test exception"));

        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        await _unitOfWork.Received(1).RollbackAsync();
    }
}
