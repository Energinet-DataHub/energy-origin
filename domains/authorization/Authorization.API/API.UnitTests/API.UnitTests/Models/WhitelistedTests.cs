using API.Models;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class WhitelistedTests
{
    [Fact]
    public void Given_ValidData_WhenCreatingWhitelisted_Then_CreatesSuccessfully()
    {
        var tin = Tin.Create("12345678");

        var whitelisted = Whitelisted.Create(tin);

        Assert.NotNull(whitelisted);
        Assert.Equal(tin, whitelisted.Tin);
    }

    [Fact]
    public void Given_Whitelisted_When_Created_Then_HasValidId()
    {
        var tin = Tin.Create("12345678");

        var whitelisted = Whitelisted.Create(tin);

        whitelisted.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Given_Whitelisted_When_Created_Then_HasCorrectTin()
    {
        var tin = Tin.Create("12345678");

        var whitelisted = Whitelisted.Create(tin);

        whitelisted.Tin.Should().Be(tin);
    }
}
