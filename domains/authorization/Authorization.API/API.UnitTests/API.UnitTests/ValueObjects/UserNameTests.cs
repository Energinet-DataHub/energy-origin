using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class UserNameTests
{
    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Null()
    {
        Action act = () => UserName.Create(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => UserName.Create(string.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Throws_ArgumentException_When_Value_Is_Whitespace()
    {
        Action act = () => UserName.Create("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null, empty, or whitespace. (Parameter 'value')");
    }

    [Fact]
    public void Name_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var name = UserName.Create("Valid Name");

        name.Value.Should().Be("Valid Name");
    }
}
