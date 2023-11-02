using API.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Helpers;

public class TimeSpanHelperTests
{

    [Theory]
    [InlineData(0, 30)]
    [InlineData(15, 15)]
    [InlineData(30, 0)]
    [InlineData(45, 45)]
    [InlineData(60, 30)]
    public void GetMinutesToNextHalfHour_Success(int startMinute, int expected)
    {
        var result = TimeSpanHelper.GetMinutesToNextHalfHour(startMinute);

        result.Should().Be(expected);
    }
}
