using FluentAssertions;
using Transfer.Domain;
using Xunit;

namespace API.UnitTests.Helpers;

public class TimeSpanHelperTests
{

    [Theory]
    [InlineData(0, 30)]
    [InlineData(15, 15)]
    [InlineData(30, 60)]
    [InlineData(45, 45)]
    [InlineData(60, 30)]
    public void GetMinutesToNextHalfHour_Success(int startMinute, int expected)
    {
        var result = new SystemTime().GetMinutesToNextHalfHour(startMinute);

        result.Should().Be(expected);
    }
}
