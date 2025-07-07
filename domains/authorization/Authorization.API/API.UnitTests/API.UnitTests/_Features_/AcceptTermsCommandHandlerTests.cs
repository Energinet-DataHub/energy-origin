using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.Data;
using API.Events;
using API.Models;
using API.Repository;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToNormal.V1;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace API.UnitTests._Features_;

public class AcceptTermsCommandHandlerTests
{
    private readonly FakeOrganizationRepository _organizationRepository;
    private readonly FakeTermsRepository _termsRepository;
    private readonly IWhitelistedRepository _whitelistedRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly AcceptTermsCommandHandler _handler;
    private readonly IWalletClient _walletClient;

    public AcceptTermsCommandHandlerTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _termsRepository = new FakeTermsRepository();
        _whitelistedRepository = new FakeWhitelistedRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _walletClient = Substitute.For<IWalletClient>();
        _walletClient.GetWalletsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new ResultList<WalletRecord>()
        { Metadata = new PageInfo() { Count = 0, Limit = 0, Offset = 0, Total = 0 }, Result = new List<WalletRecord>() });
        _walletClient.CreateWalletAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new CreateWalletResponse() { WalletId = Guid.NewGuid() });
        _handler = new AcceptTermsCommandHandler(_organizationRepository, _termsRepository, _whitelistedRepository, _unitOfWork, _walletClient, _publishEndpoint, new DomainEventDispatcher(_publishEndpoint));
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_CreatesNewOrganizationAndPublishesMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        _organizationRepository.Query().Count().Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(Arg.Is(
            (OrgAcceptedTerms msg) =>
                msg.Tin == command.OrgCvr &&
                msg.SubjectId == _organizationRepository.Query().First().Id &&
                msg.Actor == command.UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTrialEnvironment_UsesTrialTerms()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), true);

        await _termsRepository.AddAsync(Terms.Create(1, TermsType.Trial), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        var org = _organizationRepository.Query().Single();
        org.TermsAccepted.Should().BeTrue();
        org.TermsVersion.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenOrganizationExistsButTermsNotAccepted_UpdatesTermsAndPublishesMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        var organization = Organization.Create(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        var whitelisted = Whitelisted.Create(Tin.Create(command.OrgCvr));
        await _whitelistedRepository.AddAsync(whitelisted, CancellationToken.None);
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(Arg.Is<OrgAcceptedTerms>(
            msg =>
                msg.Tin == command.OrgCvr &&
                msg.SubjectId == organization.Id &&
                msg.Actor == command.UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPromotedToNormalFromTrial_UpdatesTermsAndPublishesMessages()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        var organization = Organization.CreateTrial(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        var whitelisted = Whitelisted.Create(Tin.Create(command.OrgCvr));
        await _whitelistedRepository.AddAsync(whitelisted, CancellationToken.None);
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(Arg.Is<OrgAcceptedTerms>(
            msg =>
                msg.Tin == command.OrgCvr &&
                msg.SubjectId == organization.Id &&
                msg.Actor == command.UserId
        ), Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(Arg.Is<OrganizationPromotedToNormal>(
            msg =>
                msg.OrganizationId == organization.Id
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndDoesNotPublishMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        var mockOrganizationRepository = Substitute.For<IOrganizationRepository>();
        mockOrganizationRepository.Query().Returns(_ => throw new Exception("Test exception"));
        await using var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptTermsCommandHandler(mockOrganizationRepository, _termsRepository, _whitelistedRepository, mockUnitOfWork, _walletClient, _publishEndpoint, new DomainEventDispatcher(_publishEndpoint));

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        await mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletNotCreated_RollsBackTransactionAndDoesNotPublishMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        var organization = Organization.Create(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        var whitelisted = Whitelisted.Create(Tin.Create(command.OrgCvr));
        await _whitelistedRepository.AddAsync(whitelisted, CancellationToken.None);
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        var walletClient = Substitute.For<IWalletClient>();
        walletClient.GetWalletsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new ResultList<WalletRecord>()
        { Metadata = new PageInfo() { Count = 0, Limit = 0, Offset = 0, Total = 0 }, Result = new List<WalletRecord>() });

        var handler = new AcceptTermsCommandHandler(_organizationRepository, _termsRepository, _whitelistedRepository, _unitOfWork, walletClient, _publishEndpoint, new DomainEventDispatcher(_publishEndpoint));

        await Assert.ThrowsAsync<WalletNotCreated>(() => handler.Handle(command, CancellationToken.None));
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoTermsExist_RollsBackTransactionAndDoesNotPublishMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidConfigurationException>();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletExistsAndIsDisabledButEnableFails_RollsBackTransactionAndDoesNotPublishMessage()
    {
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        var organization = Organization.Create(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        var walletClient = Substitute.For<IWalletClient>();

        var disabledWallet = new WalletRecord
        {
            Id = Guid.NewGuid(),
            PublicKey = Substitute.For<IHDPublicKey>(),
            DisabledDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        walletClient.GetWalletsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 1,
                    Limit = 1,
                    Offset = 0,
                    Total = 1
                },
                Result = new List<WalletRecord> { disabledWallet }
            });

        walletClient
            .EnableWalletAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new BusinessException("Failed to enable wallet."));

        var handler = new AcceptTermsCommandHandler(_organizationRepository, _termsRepository, _whitelistedRepository, _unitOfWork, walletClient, _publishEndpoint, new DomainEventDispatcher(_publishEndpoint));

        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(command, CancellationToken.None));
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletExistsAndIsDisabled_EnablesWalletAndPublishesMessage()
    {
        // Arrange
        var command = new AcceptTermsCommand("12345678", "Test Org", Guid.NewGuid(), IsTrial: false);
        var organization = Organization.Create(Tin.Create(command.OrgCvr), OrganizationName.Create("Test Org"));
        var whitelisted = Whitelisted.Create(Tin.Create(command.OrgCvr));
        await _whitelistedRepository.AddAsync(whitelisted, CancellationToken.None);
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _termsRepository.AddAsync(Terms.Create(1), CancellationToken.None);

        var walletClient = Substitute.For<IWalletClient>();

        var disabledWallet = new WalletRecord
        {
            Id = Guid.NewGuid(),
            PublicKey = Substitute.For<IHDPublicKey>(),
            DisabledDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        walletClient.GetWalletsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 1,
                    Limit = 1,
                    Offset = 0,
                    Total = 1
                },
                Result = new List<WalletRecord> { disabledWallet }
            });

        walletClient.EnableWalletAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EnableWalletResponse { WalletId = disabledWallet.Id });

        var handler = new AcceptTermsCommandHandler(_organizationRepository, _termsRepository, _whitelistedRepository, _unitOfWork, walletClient, _publishEndpoint, new DomainEventDispatcher(_publishEndpoint));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(Arg.Any<OrgAcceptedTerms>(), Arg.Any<CancellationToken>());
    }
}
