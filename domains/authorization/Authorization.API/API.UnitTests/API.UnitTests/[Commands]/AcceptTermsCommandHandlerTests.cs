using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using FluentAssertions;
using MassTransit;
using NSubstitute;

namespace API.UnitTests._Commands_;

public class AcceptTermsCommandHandlerTests
{
    private readonly FakeOrganizationRepository _organizationRepository;
    private readonly FakeTermsRepository _termsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly AcceptTermsCommandHandler _handler;

    public AcceptTermsCommandHandlerTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _termsRepository = new FakeTermsRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _handler = new AcceptTermsCommandHandler(_organizationRepository, _termsRepository, _unitOfWork, _publishEndpoint);
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_CreatesNewOrganizationAndPublishesMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid());
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        _organizationRepository.Query().Count().Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync();
        await _publishEndpoint.Received(1).Publish(Arg.Is<OrgAcceptedTerms>(
            (OrgAcceptedTerms msg) =>
                msg.Tin == command.OrgCvr &&
                msg.SubjectId == _organizationRepository.Query().First().Id &&
                msg.Actor == command.UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_UpdatesTermsAndPublishesMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid());
        var organization = Organization.Create(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync();
        await _publishEndpoint.Received(1).Publish(Arg.Is<OrgAcceptedTerms>(
            msg =>
                msg.Tin == command.OrgCvr &&
                msg.SubjectId == organization.Id &&
                msg.Actor == command.UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndDoesNotPublishMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid());
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(_ => throw new Exception("Test exception"));
        await using var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptTermsCommandHandler(mockOrganizationRepository, _termsRepository, mockUnitOfWork, _publishEndpoint);

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await mockUnitOfWork.DidNotReceive().CommitAsync();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoTermsExist_RollsBackTransactionAndDoesNotPublishMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid());

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidConfigurationException>();
        await _unitOfWork.DidNotReceive().CommitAsync();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }
}
