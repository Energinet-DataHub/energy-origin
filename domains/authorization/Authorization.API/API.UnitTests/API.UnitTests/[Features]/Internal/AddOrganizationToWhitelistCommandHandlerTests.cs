using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class AddOrganizationToWhitelistCommandHandlerTests
{
    private readonly FakeWhitelistedRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Given_OrganizationDoesNotExist_WhenHandlingCommand_Then_AddsOrganizationToWhitelist()
    {
        var tin = Tin.Create("12345678");
        var command = new AddOrganizationToWhitelistCommand(tin);
        var handler = new AddOrganizationToWhitelistCommandHandler(_repository, _unitOfWork);

        await handler.Handle(command, CancellationToken.None);

        _repository.Query().Should().ContainSingle(w => w.Tin == tin);
    }

    [Fact]
    public async Task Given_OrganizationAlreadyExists_WhenHandlingCommand_Then_DoesNotAdd()
    {
        var tin = Tin.Create("12345678");
        var existing = Whitelisted.Create(tin);
        await _repository.AddAsync(existing, CancellationToken.None);

        var command = new AddOrganizationToWhitelistCommand(tin);
        var handler = new AddOrganizationToWhitelistCommandHandler(_repository, _unitOfWork);

        await handler.Handle(command, CancellationToken.None);

        _repository.Query().Count(w => w.Tin == tin).Should().Be(1);
    }
}
