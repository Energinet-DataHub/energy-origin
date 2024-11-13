using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class CreateClientCommandTest
{
    private readonly FakeClientRepository _fakeClientRepository = new();
    private readonly FakeUnitOfWork _fakeUnitOfWork = new();

    [Fact]
    public void GivenValidInput_WhenCreatingClient_ThenClientIsCreated()
    {
        // Arrange
        CreateClientCommand command = new(new IdpClientId(Guid.NewGuid()), new ClientName("Test Client"), ClientType.Internal, "http://localhost:5000");
        CreateClientCommandHandler handler = new(_fakeUnitOfWork, _fakeClientRepository);

        // Act
        var result = handler.Handle(command, CancellationToken.None);
        var clients = _fakeClientRepository.Query().ToList();

        // Assert
        clients.Count.Should().Be(1);
        clients[0].IdpClientId.Should().Be(command.IdpClientId);
        clients[0].Name.Should().Be(command.Name);
        clients[0].RedirectUrl.Should().Be(command.RedirectUrl);
    }
}
