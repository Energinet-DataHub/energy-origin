using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V3;
using FluentAssertions;

namespace EnergyOrigin.IntegrationEvents.Tests.Events.EnergyMeasured.V3;

public class AddressTests
{
    [Theory]
    [InlineData("H.C. Andersens Boulevard", "34", "1", "th", "1553", "København V", "Danmark", "H.C. Andersens Boulevard 34, 1. th, 1553 København V, Danmark")]
    [InlineData("Tophøjvej", "2B", "", "", "9500", "Hobro", "Danmark", "Tophøjvej 2B, 9500 Hobro, Danmark")]
    [InlineData("Gl. Landevej", "34A", "", "", "4000", "Roskilde", "Danmark", "Gl. Landevej 34A, 4000 Roskilde, Danmark")]
    [InlineData("Vigerslev Allé", "158P", "", "", "2500", "Valby", "Danmark", "Vigerslev Allé 158P, 2500 Valby, Danmark")]
    [InlineData("Øresundsvej", "128", "1", "8", "2300", "København S", "Danmark", "Øresundsvej 128, 1. 8, 2300 København S, Danmark")]
    [InlineData("Nygårdsvænget", "34", "st", "a19", "2800", "Kongens Lyngby", "Danmark", "Nygårdsvænget 34, st. a19, 2800 Kongens Lyngby, Danmark")]
    [InlineData("   Nygårdsvænget   ", "  34  ", "  st  ", "  a19  ", "  2800  ", "  Kongens Lyngby  ", "  Danmark  ", "Nygårdsvænget 34, st. a19, 2800 Kongens Lyngby, Danmark")]
    public void ToString_WhenCalled_ShouldReturnFormattedAddress(string streetName, string buildingNumber, string floor, string room, string postcode, string cityName, string country, string expected)
    {
        var address = new Address(streetName, buildingNumber, floor, room, postcode, cityName, country);

        address.ToString().Should().Be(expected);
    }
}
