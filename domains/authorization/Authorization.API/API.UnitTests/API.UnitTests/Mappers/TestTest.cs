using API.Authorization.Controllers;
using FluentAssertions;

namespace API.UnitTests.Mappers;

public class TestTest
{

    [Fact]
    public void GivenClientType_WhenExternal_ThenShouldMapToDatabaseTypeExternal()
    {
        // Arrange
        ClientType clientType = ClientType.External;

        // Act
        var result = ClientTypeMapper.MapToDatabaseClientType(clientType);

        // Assert
        result.Should().Be(API.Models.ClientType.External);
    }

    [Fact]
    public void GivenClientType_WhenInternal_ThenShouldMapToDatabaseTypeInternal()
    {
        // Arrange
        ClientType clientType = ClientType.Internal;

        // Act
        var result = ClientTypeMapper.MapToDatabaseClientType(clientType);

        // Assert
        result.Should().Be(API.Models.ClientType.Internal);
    }

    [Fact]
    public void GivenClientType_WhenUnknown_ThenShouldThrowException()
    {
        // Arrange
        ClientType clientType = (ClientType) 2;

        // Act
        Action act = () => ClientTypeMapper.MapToDatabaseClientType(clientType);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }


}
