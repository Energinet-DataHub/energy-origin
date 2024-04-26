using API.ValueObjects;

namespace API.UnitTests.ValueObjects;

public class TinTests
{
    [Fact]
    public void Tin_WithValidData_CreatesSuccessfully()
    {
        var tin = new Tin("00000000");
        Assert.Equal("00000000", tin.Value);
    }

    [Fact]
    public void Tin_WithInvalidLength_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new Tin("123456789"));
    }

    [Fact]
    public void Tin_WithNonDigitCharacters_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new Tin("1234567a"));
    }

    [Fact]
    public void Tin_WithEmptyValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new Tin(""));
    }

    [Fact]
    public void Tin_WithNullValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new Tin(null!));
    }

    [Fact]
    public void Tin_WithWhitespaceValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new Tin(" "));
    }
}
