using Xunit;

namespace EnergyOrigin.Domain.ValueObjects.Tests;

public class TinTest
{
    [Fact]
    public void Tin_WithValidData_CreatesSuccessfully()
    {
        Assert.Equal("00000000", Tin.Create("00000000").Value);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("123456")]
    public void Tin_WithInvalidLength_ThrowsException(string value)
    {
        Assert.Throws<ArgumentException>(() => Tin.Create(value));
    }

    [Fact]
    public void Tin_WithNonDigitCharacters_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Tin.Create("1234567a"));
    }

    [Fact]
    public void Tin_WithEmptyValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Tin.Create(""));
    }

    [Fact]
    public void Tin_WithNullValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Tin.Create(null!));
    }

    [Fact]
    public void Tin_WithWhitespaceValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Tin.Create(" "));
    }
}
