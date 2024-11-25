using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class CreateClientCommandTest
{
    private readonly FakeClientRepository _fakeClientRepository = new();
    private readonly FakeOrganizationRepository _fakeOrganization = new();
    private readonly FakeUnitOfWork _fakeUnitOfWork = new();

    [Fact]
    public void GivenValidInput_WhenCreatingClient_ThenClientIsCreated()
    {
        // Arrange
        CreateClientCommand createClientCommand = new(new IdpClientId(Guid.NewGuid()), new ClientName("Test Client"), ClientType.External, "http://localhost:5000");
        CreateClientCommandHandler createClientCommandHandler = new(_fakeUnitOfWork, _fakeClientRepository, _fakeOrganization);

        // Act
        var result = createClientCommandHandler.Handle(createClientCommand, CancellationToken.None);
        var clients = _fakeClientRepository.Query().ToList();
        var organizations = _fakeOrganization.Query().ToList();

        // Assert Client
        clients.Count.Should().Be(1);
        clients[0].IdpClientId.Should().Be(createClientCommand.IdpClientId);
        clients[0].Name.Should().Be(createClientCommand.Name);
        clients[0].RedirectUrl.Should().Be(createClientCommand.RedirectUrl);
        clients[0].ClientType.Should().Be(ClientType.External);

        // Assert Organization
        organizations.Count.Should().Be(1);
        organizations[0].Tin.Should().Be(null);
        organizations[0].ServiceProviderTermsAccepted.Should().BeTrue();
        organizations[0].ServiceProviderTermsAcceptanceDate.Should().NotBeNull();
        organizations[0].TermsAccepted.Should().BeFalse();
        organizations[0].TermsVersion.Should().BeNull();
        organizations[0].TermsAcceptanceDate.Should().BeNull();
        organizations[0].Affiliations.Should().BeEmpty();

        // Assert relation between Client and Organization
        clients[0].OrganizationId!.Value.Should().Be(organizations[0].Id);
        organizations[0].Name.Value.Should().Be(clients[0].Name.Value);
    }
}
