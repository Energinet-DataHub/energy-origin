using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using NSubstitute;

namespace API.UnitTests._Features_.Internal;

public class RemoveFromWhitelistCommandHandlerTest
{
    private readonly FakeWhitelistedRepository _fakeWhitelistRepo;
    private readonly FakeOrganizationRepository _fakeOrganizationRepo;
    private readonly FakeUnitOfWork _fakeUnitOfWork;
    private readonly IPublishEndpoint _fakePublishEndpoint;
    private readonly RemoveOrganizationFromWhitelistCommandHandler _sut;

    public RemoveFromWhitelistCommandHandlerTest()
    {
        _fakeWhitelistRepo = new FakeWhitelistedRepository();
        _fakeOrganizationRepo = new FakeOrganizationRepository();
        _fakeUnitOfWork = new FakeUnitOfWork();
        _fakePublishEndpoint = Substitute.For<IPublishEndpoint>();
        _sut = new RemoveOrganizationFromWhitelistCommandHandler(_fakeWhitelistRepo, _fakeOrganizationRepo, _fakePublishEndpoint, _fakeUnitOfWork);
    }

    [Fact]
    public async Task GivenNonExistingTin_WhenRemoving_NothingHappens()
    {
        await _sut.Handle(new RemoveFromWhitelistCommand("77777777"), CancellationToken.None);
        _fakePublishEndpoint.Received(0);
    }

    [Fact]
    public async Task GivenWhiteListedTin_WhenRemoving_TinIsRemovedFromWhitelist()
    {
        await _fakeWhitelistRepo.AddAsync(Whitelisted.Create(Tin.Create("88888888")), CancellationToken.None);
        await _sut.Handle(new RemoveFromWhitelistCommand("88888888"), CancellationToken.None);

        _fakePublishEndpoint.Received(0);
        Assert.Equal(0, _fakeWhitelistRepo.Query().Count());
    }

    [Fact]
    public async Task GivenWhiteListedTinAndOrganization_WhenRemoving_TinIsRemovedFromWhitelist()
    {
        await _fakeOrganizationRepo.AddAsync(Organization.Create(Tin.Create("88888888"), OrganizationName.Create("Acme Inc.")),
            CancellationToken.None);
        await _fakeWhitelistRepo.AddAsync(Whitelisted.Create(Tin.Create("88888888")), CancellationToken.None);
        await _sut.Handle(new RemoveFromWhitelistCommand("88888888"), CancellationToken.None);

        await _fakePublishEndpoint.Received(1).Publish(Arg.Any<OrganizationRemovedFromWhitelist>(), Arg.Any<CancellationToken>());
        Assert.Equal(0, _fakeWhitelistRepo.Query().Count());
        Assert.Equal(1, _fakeOrganizationRepo.Query().Count());
    }
}
