using API.Authorization._Features_.Internal;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using NSubstitute;

namespace API.UnitTests._Features_.Internal;

public class WhitelistOrganizationCommandHandlerTests
{
    private readonly IWhitelistedRepository _whitelistedRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WhitelistOrganizationCommandHandler _handler;

    public WhitelistOrganizationCommandHandlerTests()
    {
        _whitelistedRepository = Substitute.For<IWhitelistedRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new WhitelistOrganizationCommandHandler(_whitelistedRepository, _unitOfWork);
    }

    [Fact]
    public async Task Given_OrganizationDoesNotExist_WhenHandlingCommand_Then_AddsOrganizationToWhitelist()
    {
        var tin = Tin.Create("12345678");
        _whitelistedRepository.Query().Returns(Enumerable.Empty<Whitelisted>().AsQueryable());

        var command = new WhitelistOrganizationCommand(tin);

        await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).BeginTransactionAsync();
        await _whitelistedRepository.Received(1).AddAsync(Arg.Is<Whitelisted>(w => w.Tin == tin), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task Given_OrganizationAlreadyExists_WhenHandlingCommand_Then_DoesNotAddAndCommits()
    {
        var tin = Tin.Create("12345678");
        var existing = Whitelisted.Create(tin);

        _whitelistedRepository.Query().Returns(new[] { existing }.AsQueryable());

        var command = new WhitelistOrganizationCommand(tin);

        await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).BeginTransactionAsync();
        await _whitelistedRepository.DidNotReceive().AddAsync(Arg.Any<Whitelisted>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync();
    }
}
