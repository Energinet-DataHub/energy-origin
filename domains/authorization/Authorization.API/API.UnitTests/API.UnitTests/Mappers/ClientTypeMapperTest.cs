using API.Authorization.Controllers;
using FluentAssertions;

namespace API.UnitTests.Mappers;

public class ClientTypeMapperTest
{

    [Fact]
    public void GivenApiClientType_WhenExternal_ThenShouldMapToDatabaseTypeExternal()
    {
        // Arrange
        ClientType clientType = ClientType.External;

        // Act
        var result = ClientTypeMapper.MapToDatabaseClientType(clientType);

        // Assert
        result.Should().Be(API.Models.ClientType.External);
    }

    [Fact]
    public void GivenApiClientType_WhenInternal_ThenShouldMapToDatabaseTypeInternal()
    {
        // Arrange
        ClientType clientType = ClientType.Internal;

        // Act
        var result = ClientTypeMapper.MapToDatabaseClientType(clientType);

        // Assert
        result.Should().Be(API.Models.ClientType.Internal);
    }

    [Fact]
    public void GivenApiClientType_WhenUnknown_ThenShouldThrowException()
    {
        // Arrange
        ClientType clientType = (ClientType)1337;

        // Act
        Action act = () => ClientTypeMapper.MapToDatabaseClientType(clientType);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GivenDatabaseClientType_WhenExternal_ThenShouldMapToApiTypeExternal()
    {
        // Arrange
        API.Models.ClientType clientType = API.Models.ClientType.External;

        // Act
        var result = ClientTypeMapper.MapToApiClientType(clientType);

        // Assert
        result.Should().Be(ClientType.External);
    }

    [Fact]
    public void GivenDatabaseClientType_WhenInternal_ThenShouldMapToApiTypeInternal()
    {
        // Arrange
        API.Models.ClientType clientType = API.Models.ClientType.Internal;

        // Act
        var result = ClientTypeMapper.MapToApiClientType(clientType);

        // Assert
        result.Should().Be(ClientType.Internal);
    }

    [Fact]
    public void GivenDatabaseClientType_WhenUnknown_ThenShouldThrowException()
    {
        // Arrange
        API.Models.ClientType clientType = (API.Models.ClientType)1337;

        // Act
        Action act = () => ClientTypeMapper.MapToApiClientType(clientType);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
