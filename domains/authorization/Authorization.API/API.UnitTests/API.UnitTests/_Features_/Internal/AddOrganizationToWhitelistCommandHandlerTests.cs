using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class AddOrganizationToWhitelistCommandHandlerTests
{
    private readonly FakeWhitelistedRepository _whitelistedRepository = new();
    private readonly FakeOrganizationRepository _orgRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Given_OrganizationDoesNotExist_WhenHandlingCommand_Then_AddsOrganizationToWhitelist()
    {
        var tin = Tin.Create("12345678");
        var command = new AddOrganizationToWhitelistCommand(tin);
        var handler = new AddOrganizationToWhitelistCommandHandler(_whitelistedRepository, _orgRepository, _unitOfWork);

        await handler.Handle(command, CancellationToken.None);

        _whitelistedRepository.Query().Should().ContainSingle(w => w.Tin == tin);
    }

    [Fact]
    public async Task Handle_WhenOrganizationIsKnown_InvalidatesTerms()
    {
        var tin = Tin.Create("12345678");
        var org = Any.Organization(tin);
        await _orgRepository.AddAsync(org, new CancellationToken());

        var command = new AddOrganizationToWhitelistCommand(tin);
        var handler = new AddOrganizationToWhitelistCommandHandler(_whitelistedRepository, _orgRepository, _unitOfWork);

        await handler.Handle(command, CancellationToken.None);

        _whitelistedRepository.Query().Should().ContainSingle(w => w.Tin == tin);
        _orgRepository.Query().Should().ContainSingle(w => w.Tin == tin && w.TermsAccepted == false);
    }
}
