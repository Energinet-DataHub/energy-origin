using API.Services;
using EnergyOrigin.Setup.Exceptions;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace API.UnitTests.Service;

public class CredentialServiceTest
{
    private readonly IGraphServiceClientWrapper _graphServiceClientWrapper;
    private readonly CredentialService _sut;

    public CredentialServiceTest()
    {
        _graphServiceClientWrapper = Substitute.For<IGraphServiceClientWrapper>();
        _sut = new CredentialService(_graphServiceClientWrapper);
    }

    [Fact]
    public async Task GivenGetCredentials_WithApplicationThatDoesNotExists_ThrowException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        ReturnNullApplication(applicationId);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.GetCredentials(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
    }

    [Fact]
    public async Task GivenGetCredentials_WithODataError_ThrowException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        ThrowODataErrorWhenGettingApplication(applicationId);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.GetCredentials(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
    }

    [Fact]
    public async Task GivenGetCredentials_NoPasswordCredentials_ReturnEmptyList()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var application = new Application
        {
            Id = applicationId.ToString(),
            PasswordCredentials = null
        };

        _graphServiceClientWrapper
            .GetApplication(applicationId.ToString(), Arg.Any<CancellationToken>())
            .Returns(application);

        // Act
        var result = await _sut.GetCredentials(applicationId, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GivenGetCredentials_WithPasswordsFromGraph_ReturnCredentials()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var startDateTime = new DateTimeOffset(2025, 4, 9, 0, 0, 0, new TimeSpan(0));
        var passwordCredential = new PasswordCredential
        {
            KeyId = Guid.NewGuid(),
            Hint = "hint",
            StartDateTime = startDateTime,
            EndDateTime = startDateTime.AddHours(1)
        };
        var application = new Application { Id = applicationId.ToString(), PasswordCredentials = [passwordCredential] };

        _graphServiceClientWrapper
            .GetApplication(applicationId.ToString(), Arg.Any<CancellationToken>())
            .Returns(application);

        // Act
        var result = await _sut.GetCredentials(applicationId, CancellationToken.None);

        // Assert
        var clientCredentials = result.ToList();
        Assert.Equal(passwordCredential.Hint, clientCredentials.First().Hint);
        Assert.Equal(passwordCredential.KeyId, clientCredentials.First().KeyId);
        Assert.Equal(passwordCredential.StartDateTime, clientCredentials.First().StartDateTime);
        Assert.Equal(passwordCredential.EndDateTime, clientCredentials.First().EndDateTime);
        Assert.Null(clientCredentials.First().Secret);
    }

    [Fact]
    public async Task GivenCreateCredential_WithApplicationThatDoesNotExists_ThrowException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        ReturnNullApplication(applicationId);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.CreateCredential(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
    }

    [Fact]
    public async Task GivenCreateCredential_WithODataError_ThrowException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        ThrowODataErrorWhenGettingApplication(applicationId);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.CreateCredential(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
    }

    [Theory]
    [InlineData("d1b8686c-778a-4962-a90d-6a7c33862e95", null)]
    [InlineData(null, "Secret")]
    public async Task GivenCreateCredential_CreatedPasswordMissingData_ThrowException(string? keyId, string? secretText)
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var application = new Application
        {
            Id = applicationId.ToString()
        };

        _graphServiceClientWrapper
            .GetApplication(applicationId.ToString(), Arg.Any<CancellationToken>())
            .Returns(application);

        _graphServiceClientWrapper
            .AddPassword(applicationId.ToString(), Arg.Any<AddPasswordPostRequestBody>(), Arg.Any<CancellationToken>())
            .Returns(new PasswordCredential
            {
                KeyId = keyId is null ? null : new Guid(keyId),
                SecretText = secretText
            });

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.CreateCredential(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<BusinessException>(exception);
    }

    [Fact]
    public async Task GivenCreateCredential_ODataErrorWhenAddingPassword_ThrowException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var application = new Application
        {
            Id = applicationId.ToString()
        };

        _graphServiceClientWrapper
            .GetApplication(Arg.Is(applicationId.ToString()), Arg.Any<CancellationToken>())
            .Returns(application);

        _graphServiceClientWrapper
            .AddPassword(applicationId.ToString(), Arg.Any<AddPasswordPostRequestBody>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ODataError());

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.CreateCredential(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<BusinessException>(exception);
    }

    [Fact]
    public async Task GivenCreateCredential_ODataErrorWhenAddingPasswordWithTooManyPasswords_ThrowException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var application = new Application
        {
            Id = applicationId.ToString()
        };

        _graphServiceClientWrapper
            .GetApplication(applicationId.ToString(), Arg.Any<CancellationToken>())
            .Returns(application);

        _graphServiceClientWrapper
            .AddPassword(applicationId.ToString(), Arg.Any<AddPasswordPostRequestBody>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ODataError
            {
                Error = new MainError
                {
                    Code = "TooManyAppPasswords"
                }
            });

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _sut.CreateCredential(applicationId, CancellationToken.None));

        // Assert
        Assert.IsType<BusinessException>(exception);
        Assert.Contains("Not allowed to add 3 credentials", exception.Message);
    }

    private void ReturnNullApplication(Guid applicationId)
    {
        _graphServiceClientWrapper
            .GetApplication(applicationId.ToString(), Arg.Any<CancellationToken>())
            .Returns((Application?)null);
    }

    private void ThrowODataErrorWhenGettingApplication(Guid applicationId)
    {
        _graphServiceClientWrapper
            .GetApplication(applicationId.ToString(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ODataError());
    }
}
