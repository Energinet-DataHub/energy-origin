using DomainCertificate.ValueObjects;
using Xunit;

namespace DomainCertificate.Tests.ValueObjects
{
    public class PeriodTests
    {
        [Theory]
        [InlineData(1,2)]
        [InlineData(1, 100)]
        public void Ctor_Success(long dateFromSeconds, long dateToSeconds)
        {
            var period = new Period(dateFromSeconds, dateToSeconds);

            Assert.Equal(period.DateFrom, dateFromSeconds);
            Assert.Equal(period.DateTo, dateToSeconds);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(200, 1)]
        public void Ctor_Fail(long dateFromSeconds, long dateToSeconds)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var period = new Period(dateFromSeconds, dateToSeconds);
            });
        }

        [Fact]
        public void ToDateInterval()
        {
            var threeMinAnd20Seconds = new TimeSpan(0, 3, 20);
            var period = new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddSeconds(200).ToUnixTimeSeconds());

            var dateInterval = period.ToDateInterval();

            Assert.Equal(threeMinAnd20Seconds, dateInterval.Duration);
        }
    }
}
