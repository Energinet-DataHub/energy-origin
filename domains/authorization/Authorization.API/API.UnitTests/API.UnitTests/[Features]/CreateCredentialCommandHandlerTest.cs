using API.Authorization._Features_;
using API.Models;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup.Exceptions;
using NSubstitute;

namespace API.UnitTests._Features_;

public class CreateCredentialCommandHandlerTest
{
    private readonly ICredentialService _credentialService;
    private readonly IClientRepository _clientRepository;
    private readonly CreateCredentialCommandHandler _sut;

    public CreateCredentialCommandHandlerTest()
    {
        _credentialService = Substitute.For<ICredentialService>();
        _clientRepository = Substitute.For<IClientRepository>();
        _sut = new CreateCredentialCommandHandler(_credentialService, _clientRepository);
    }

    [Fact]
    public async Task GivenCreateCredential_WithNoAccessThroughOrganization_ThrowsForbiddenException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new CreateCredentialCommand(clientId, organizationId);

        _clientRepository.ExternalClientHasAccessThroughOrganization(clientId, organizationId).Returns(false);

        // Act
        var exception = await Record.ExceptionAsync(async () => await _sut.Handle(command, CancellationToken.None));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task GivenCreateCredential_WithAccessThroughOrganization_ReturnCreatedCredential()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new CreateCredentialCommand(clientId, organizationId);

        _clientRepository.ExternalClientHasAccessThroughOrganization(clientId, organizationId).Returns(true);

        var startDateTime = new DateTimeOffset(2025, 4, 9, 0, 0, 0, new TimeSpan(0));
        var endDateTime = startDateTime.AddHours(1);
        var hint = "hint";
        var keyId = Guid.NewGuid();
        var secret = "secret";
        _credentialService.CreateCredential(clientId, CancellationToken.None)
            .Returns(ClientCredential.Create(hint, keyId, startDateTime, endDateTime, secret));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(hint, result.Hint);
        Assert.Equal(keyId, result.KeyId);
        Assert.Equal(startDateTime, result.StartDateTime);
        Assert.Equal(endDateTime, result.EndDateTime);
        Assert.Equal(secret, result.Secret);
    }
}
