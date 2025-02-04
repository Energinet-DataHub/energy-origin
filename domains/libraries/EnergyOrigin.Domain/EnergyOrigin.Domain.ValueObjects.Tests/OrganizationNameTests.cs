using Xunit;

namespace EnergyOrigin.Domain.ValueObjects.Tests;

public class OrganizationNameTests
{
    [Fact]
    public void OrganizationName_WithValidData_CreatesSuccessfully()
    {
        var organizationName = OrganizationName.Create("Test Organization");
        Assert.Equal("Test Organization", organizationName.Value);
    }

    [Fact]
    public void OrganizationName_WithWhitespace_TrimsSuccessfully()
    {
        var organizationName = OrganizationName.Create(" Test Organization ");
        Assert.Equal("Test Organization", organizationName.Value);
    }

    [Fact]
    public void OrganizationName_WithEmptyValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => OrganizationName.Create(""));
    }

    [Fact]
    public void OrganizationName_WithNullValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => OrganizationName.Create(null!));
    }

    [Fact]
    public void OrganizationName_WithDanishStockCompanyEnding_ShouldBeAccepted()
    {
        var organizationName = OrganizationName.Create("Producent A/S");
        Assert.Equal("Producent A/S", organizationName.Value);
    }
}
