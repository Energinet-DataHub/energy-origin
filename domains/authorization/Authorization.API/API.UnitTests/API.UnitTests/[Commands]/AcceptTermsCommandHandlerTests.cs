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
    private readonly FakeTermsRepository _termsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AcceptTermsCommandHandler _handler;

    public AcceptTermsCommandHandlerTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _termsRepository = new FakeTermsRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AcceptTermsCommandHandler(_organizationRepository, _termsRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_CreatesNewOrganization()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org");
        await _termsRepository.AddAsync(Terms.Create("1.0"), CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _organizationRepository.Query().Count().Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_UpdatesTerms()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org");
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
    public async Task Handle_WhenExceptionOccurs_RollsBackTransaction()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org");
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(_ => throw new Exception("Test exception"));
        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptTermsCommandHandler(mockOrganizationRepository, _termsRepository, mockUnitOfWork);

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await mockUnitOfWork.Received(1).RollbackAsync();
    }
}
