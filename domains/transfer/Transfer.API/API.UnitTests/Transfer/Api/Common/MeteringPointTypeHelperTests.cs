using API.Transfer.Api.Common;
using Xunit;

namespace API.UnitTests.Transfer.Api.Common;

public class MeteringPointTypeHelperTests
{
    [InlineData("")]
    [InlineData("E18")]
    [InlineData("e18")]
    [Theory]
    public void GivenIsConsumption_WhenNonConsumptionMeteringPointType_ReturnsFalse(string meteringPointType)
    {
        // Act
        var isConsumption = MeteringPointTypeHelper.IsConsumption(meteringPointType);

        // Assert
        Assert.False(isConsumption);
    }

    [InlineData("E17")]
    [InlineData("e17")]
    [Theory]
    public void GivenIsConsumption_WhenConsumptionMeteringPointType_ReturnsTrue(string meteringPointType)
    {
        // Act
        var isConsumption = MeteringPointTypeHelper.IsConsumption(meteringPointType);

        // Assert
        Assert.True(isConsumption);
    }
}
