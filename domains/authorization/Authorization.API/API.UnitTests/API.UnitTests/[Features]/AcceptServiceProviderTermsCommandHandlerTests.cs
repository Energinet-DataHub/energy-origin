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
    private readonly IUnitOfWork _unitOfWork;
    private readonly AcceptServiceProviderTermsCommandHandler _handler;

    public AcceptServiceProviderTermsCommandHandlerTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AcceptServiceProviderTermsCommandHandler(_organizationRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenOrganizationNotAcceptedServiceProviderTerms_AcceptsServiceProviderTerms()
    {
        var organization = Organization.Create(Any.Tin(), Any.OrganizationName());
        var command = new AcceptServiceProviderTermsCommand(OrganizationId.Create(organization.Id));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        var updatedOrganization = _organizationRepository.Query().FirstOrDefault(o => o.Id == organization.Id);
        updatedOrganization.Should().NotBeNull();
        updatedOrganization!.ServiceProviderTermsAccepted.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndDoesNotSaveChanges()
    {
        var organization = Organization.Create(Any.Tin(), Any.OrganizationName());
        var command = new AcceptServiceProviderTermsCommand(OrganizationId.Create(organization.Id));
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(_ => throw new Exception("Test exception"));
        await using var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptServiceProviderTermsCommandHandler(mockOrganizationRepository, mockUnitOfWork);

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await mockUnitOfWork.DidNotReceive().CommitAsync();
    }

    [Fact]
    public async Task Handle_WhenNoOrganizationExists_RollsBackTransaction()
    {
        var organization = Organization.Create(Any.Tin(), Any.OrganizationName());
        var command = new AcceptServiceProviderTermsCommand(OrganizationId.Create(organization.Id));

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
        await _unitOfWork.DidNotReceive().CommitAsync();
    }
}
