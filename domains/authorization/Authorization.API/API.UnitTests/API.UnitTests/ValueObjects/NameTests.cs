using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class NameTests
{
    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Null()
    {
        Action act = () => Name.Create(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => Name.Create(string.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Whitespace()
    {
        Action act = () => Name.Create("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var name = Name.Create("Valid Name");

        name.Value.Should().Be("Valid Name");
    }
}
