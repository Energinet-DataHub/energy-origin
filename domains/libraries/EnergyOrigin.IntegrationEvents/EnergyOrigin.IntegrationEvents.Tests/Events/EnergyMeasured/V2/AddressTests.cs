using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V2;
using FluentAssertions;

namespace EnergyOrigin.IntegrationEvents.Tests.Events.EnergyMeasured.V2;

public class AddressTests
{
    [Fact]
    public void ToString_WhenCalled_ShouldReturnFormattedAddress()
    {
        var address = new Address("Vesterballevej", "4", "Fredericia", "7000", "Denmark");

        address.ToString().Should().Be("Vesterballevej 4. 7000 Fredericia, Denmark");
    }
}
