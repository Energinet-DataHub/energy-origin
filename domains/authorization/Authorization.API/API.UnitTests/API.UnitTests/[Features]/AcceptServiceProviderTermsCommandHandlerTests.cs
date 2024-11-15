using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Features_;

public class AcceptServiceProviderTermsCommandHandlerTests
{
    private readonly FakeOrganizationRepository _organizationRepository;
    private readonly FakeServiceProviderTermsRepository _serviceProviderTermsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AcceptServiceProviderTermsCommandHandler _handler;

    public AcceptServiceProviderTermsCommandHandlerTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _serviceProviderTermsRepository = new FakeServiceProviderTermsRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AcceptServiceProviderTermsCommandHandler(_organizationRepository, _serviceProviderTermsRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenOrganizationNotAcceptedServiceProviderTerms_AcceptsServiceProviderTerms()
    {
        var command = new AcceptServiceProviderTermsCommand("12345678");
        await _serviceProviderTermsRepository.AddAsync(ServiceProviderTerms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        _organizationRepository.Query().Count().Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_UpdatesTerms()
    {
        var command = new AcceptServiceProviderTermsCommand("12345678");
        var organization = Organization.Create(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _serviceProviderTermsRepository.AddAsync(ServiceProviderTerms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        organization.ServiceProviderTermsAccepted.Should().BeTrue();
        organization.ServiceProviderTermsAccepted.Should().Be(true);
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndDoesNotSaveChanges()
    {
        var command = new AcceptServiceProviderTermsCommand("12345678");
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(_ => throw new Exception("Test exception"));
        await using var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptServiceProviderTermsCommandHandler(mockOrganizationRepository, _serviceProviderTermsRepository, mockUnitOfWork);

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await mockUnitOfWork.DidNotReceive().CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenNoServiceProviderTermsExist_RollsBackTransaction()
    {
        var command = new AcceptServiceProviderTermsCommand("12345678");

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidConfigurationException>();
        await _unitOfWork.DidNotReceive().CommitAsync();
    }
}
