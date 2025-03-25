using API.Authorization._Features_.Internal;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MockQueryable;
using NSubstitute;

namespace API.UnitTests._Features_.Internal;

public class AddOrganizationToWhitelistCommandHandlerTests
{
    private readonly IWhitelistedRepository _whitelistedRepository;
    private readonly AddOrganizationToWhitelistCommandHandler _handler;

    public AddOrganizationToWhitelistCommandHandlerTests()
    {
        _whitelistedRepository = Substitute.For<IWhitelistedRepository>();
        _handler = new AddOrganizationToWhitelistCommandHandler(_whitelistedRepository);
    }

    [Fact]
    public async Task Given_OrganizationDoesNotExist_WhenHandlingCommand_Then_AddsOrganizationToWhitelist()
    {
        var tin = Tin.Create("12345678");

        var mock = new List<Whitelisted>()
            .AsQueryable()
            .BuildMock();

        _whitelistedRepository.Query().Returns(mock);

        var command = new AddOrganizationToWhitelistCommand(tin);

        await _handler.Handle(command, CancellationToken.None);

        await _whitelistedRepository.Received(1).AddAsync(
            Arg.Is<Whitelisted>(w => w.Tin == tin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_OrganizationAlreadyExists_WhenHandlingCommand_Then_DoesNotAdd()
    {
        var tin = Tin.Create("12345678");
        var existing = Whitelisted.Create(tin);

        var mock = new List<Whitelisted> { existing }
            .AsQueryable()
            .BuildMock();

        _whitelistedRepository.Query().Returns(mock);

        var command = new AddOrganizationToWhitelistCommand(tin);

        await _handler.Handle(command, CancellationToken.None);

        await _whitelistedRepository.DidNotReceive().AddAsync(Arg.Any<Whitelisted>(), Arg.Any<CancellationToken>());
    }
}
