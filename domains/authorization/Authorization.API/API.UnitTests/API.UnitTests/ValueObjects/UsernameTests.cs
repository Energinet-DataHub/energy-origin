using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class UsernameTests
{
    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Null()
    {
        Action act = () => Username.Create(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => Username.Create(string.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Whitespace()
    {
        Action act = () => Username.Create("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var name = Username.Create("Valid Name");

        name.Value.Should().Be("Valid Name");
    }
}
