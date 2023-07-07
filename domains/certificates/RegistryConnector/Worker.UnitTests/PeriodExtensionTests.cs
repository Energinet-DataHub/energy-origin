using System;
using CertificateValueObjects;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace RegistryConnector.Worker.UnitTests;

public class PeriodExtensionTests
{
    [Fact]
    public void ToDateInterval()
    {
        var threeMinAnd20Seconds = new TimeSpan(0, 3, 20);
        var period = new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddSeconds(200).ToUnixTimeSeconds());

        var dateInterval = period.ToDateInterval();

        var duration = dateInterval.End - dateInterval.Start;
            
        duration. Should().Be(Duration.FromTimeSpan(threeMinAnd20Seconds));
    }
}
