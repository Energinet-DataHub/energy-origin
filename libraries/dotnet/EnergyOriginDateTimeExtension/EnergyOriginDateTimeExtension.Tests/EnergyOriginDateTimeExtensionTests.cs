using System;
using Xunit;

namespace EnergyOriginDateTimeExtension.Tests
{
    public class EnergyOriginDateTimeExtensionTests
    {
        [Fact]
        public void ValidateToUnixTime_GivenValidDateTime_CorrectConvertion()
        {
            var dateTime = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
            long expectedTime = 1609538400;

            Assert.Equal(EnergyOriginDateTimeExtension.ToUnixTime(dateTime), expectedTime);
        }

        [Fact]
        public void ValidateToDateTime_GivenValidUnixDateTime_CorrectConvertion()
        {
            long dateTime = 1609538400;
            var expectedTime = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);

            Assert.Equal(EnergyOriginDateTimeExtension.ToDateTime(dateTime), expectedTime);
        }

        [Fact]
        public void ValidateToUnixTime_GivenNonMatchingDateTime_Fail()
        {
            var dateTime = new DateTime(2021, 1, 1, 23, 0, 0, DateTimeKind.Utc);
            long expectedTime = 1609538400;

            Assert.NotEqual(EnergyOriginDateTimeExtension.ToUnixTime(dateTime), expectedTime);
        }

        [Fact]
        public void ValidateToDateTime_GivenNonMatchingUnixDateTime_Fail()
        {
            long dateTime = 1609538401;
            var expectedTime = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);

            Assert.NotEqual(EnergyOriginDateTimeExtension.ToDateTime(dateTime), expectedTime);
        }
    }
}
