using API.Authorization._Features_;
using API.Models;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup.Exceptions;
using NSubstitute;

namespace API.UnitTests._Features_;

public class GetCredentialsQueryHandlerTest
{
    private readonly ICredentialService _credentialService;
    private readonly IClientRepository _clientRepository;
    private readonly GetCredentialsQueryHandler _sut;

    public GetCredentialsQueryHandlerTest()
    {
        _credentialService = Substitute.For<ICredentialService>();
        _clientRepository = Substitute.For<IClientRepository>();
        _sut = new GetCredentialsQueryHandler(_credentialService, _clientRepository);
    }

    [Fact]
    public async Task GivenGetCredentials_WithNoAccessThroughOrganization_ThrowsForbiddenException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var query = new GetCredentialsQuery(clientId, organizationId);

        _clientRepository.ExternalClientHasAccessThroughOrganization(clientId, organizationId).Returns(false);

        // Act
        var exception = await Record.ExceptionAsync(
            (Func<Task>)(async () => await _sut.Handle(query, CancellationToken.None)));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task GivenGetCredentials_WithAccessThroughOrganization_ReturnCredentials()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var query = new GetCredentialsQuery(clientId, organizationId);

        _clientRepository.ExternalClientHasAccessThroughOrganization(clientId, organizationId).Returns(true);

        var startDateTime = new DateTimeOffset(2025, 4, 9, 0, 0, 0, new TimeSpan(0));
        var endDateTime = startDateTime.AddHours(1);
        var hint = "hint";
        var keyId = Guid.NewGuid();
        _credentialService.GetCredentials(clientId, Arg.Any<CancellationToken>())
            .Returns(new List<ClientCredential>
            {
                ClientCredential.Create(hint, keyId, startDateTime, endDateTime)
            });

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var getCredentialsQueryResults = result.ToList();
        Assert.Equal(hint, getCredentialsQueryResults.FirstOrDefault()!.Hint);
        Assert.Equal(keyId, getCredentialsQueryResults.FirstOrDefault()!.KeyId);
        Assert.Equal(startDateTime, getCredentialsQueryResults.FirstOrDefault()!.StartDateTime);
        Assert.Equal(endDateTime, getCredentialsQueryResults.FirstOrDefault()!.EndDateTime);
    }
}
