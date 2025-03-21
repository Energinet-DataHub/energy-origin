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
        whitelisted.Id.Should().NotBe(Guid.Empty);
        whitelisted.Tin.Should().Be(tin);
    }
}
