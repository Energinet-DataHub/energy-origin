using API.Authorization._Features_;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup.Exceptions;
using NSubstitute;

namespace API.UnitTests._Features_;

public class DeleteCredentialCommandHandlerTest
{
    private readonly ICredentialService _credentialService;
    private readonly IClientRepository _clientRepository;
    private readonly DeleteCredentialCommandHandler _sut;

    public DeleteCredentialCommandHandlerTest()
    {
        _credentialService = Substitute.For<ICredentialService>();
        _clientRepository = Substitute.For<IClientRepository>();
        _sut = new DeleteCredentialCommandHandler(_credentialService, _clientRepository);
    }

    [Fact]
    public async Task GivenDeleteCredential_WithNoAccessThroughOrganization_ThrowsForbiddenException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new DeleteCredentialCommand(clientId, keyId, organizationId);

        _clientRepository.ExternalClientHasAccessThroughOrganization(clientId, organizationId).Returns(false);

        // Act
        var exception =
            await Record.ExceptionAsync((Func<Task>)(async () => await _sut.Handle(command, CancellationToken.None)));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task GivenDeleteCredential_WithAccessThroughOrganization_DeletesCredential()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new DeleteCredentialCommand(clientId, keyId, organizationId);

        _clientRepository.ExternalClientHasAccessThroughOrganization(clientId, organizationId).Returns(true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _credentialService.Received().DeleteCredential(clientId, keyId, Arg.Any<CancellationToken>());
    }
}
