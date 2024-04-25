using API.ValueObjects;

namespace API.UnitTests.ValueObjects;

public class OrganizationNameTests
{
    [Fact]
    public void OrganizationName_WithValidData_CreatesSuccessfully()
    {
        var organizationName = new OrganizationName("Test Organization");
        Assert.Equal("Test Organization", organizationName.Value);
    }

    [Fact]
    public void OrganizationName_WithWhitespace_TrimsSuccessfully()
    {
        var organizationName = new OrganizationName(" Test Organization ");
        Assert.Equal("Test Organization", organizationName.Value);
    }

    [Fact]
    public void OrganizationName_WithInvalidCharacters_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new OrganizationName("Test Organization!"));
    }

    [Fact]
    public void OrganizationName_WithExceedingLength_ThrowsException()
    {
        var longName = new string('a', 101);
        Assert.Throws<ArgumentException>(() => new OrganizationName(longName));
    }

    [Fact]
    public void OrganizationName_WithEmptyValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new OrganizationName(""));
    }

    [Fact]
    public void OrganizationName_WithNullValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new OrganizationName(null!));
    }
}
