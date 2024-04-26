using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class NameTests
{
    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Null()
    {
        Action act = () => new Name(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => new Name(string.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Whitespace()
    {
        Action act = () => new Name("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var name = new Name("Valid Name");

        name.Value.Should().Be("Valid Name");
    }
}
