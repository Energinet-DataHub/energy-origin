using System;
using API.Options;
using API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.UnitTests.Services;

public class WalletDepositEndpointServiceTests
{
    private WalletDepositEndpointService service;
    private Mock<IOptions<ProjectOriginOptions>> mockProjectOriginOptions;
    private Mock<ILogger<WalletDepositEndpointService>> mockLogger;

    public WalletDepositEndpointServiceTests()
    {
        mockProjectOriginOptions = new Mock<IOptions<ProjectOriginOptions>>();
        mockLogger = new Mock<ILogger<WalletDepositEndpointService>>();
        service =  new WalletDepositEndpointService(mockProjectOriginOptions.Object, mockLogger.Object);
    }

    [Fact]
    public void ConvertUuidToGuid_ThrowsArgumentException_WhenUuidValueIsEmpty()
    {
        var emptyUuidValue = new byte[16];
        var guidFromBytes = new Guid(emptyUuidValue);
        var receiverId = new ProjectOrigin.Common.V1.Uuid { Value = guidFromBytes.ToString() };

        Action action = () => service.ConvertUuidToGuid(receiverId);

        action.Should().Throw<ArgumentException>()
            .WithMessage("The receiver Id cannot be an empty Guid. (Parameter 'receiverId')");
    }

    [Fact]
    public void ConvertUuidToGuid_ReturnsExpectedGuid_WhenUuidValueIsNotEmpty()
    {
        var validUuidValue = Guid.NewGuid().ToByteArray();
        var guidFromBytes = new Guid(validUuidValue);
        var receiverId = new ProjectOrigin.Common.V1.Uuid { Value = guidFromBytes.ToString() };

        var result = service.ConvertUuidToGuid(receiverId);

        result.Should().Be(new Guid(validUuidValue));
    }
}
