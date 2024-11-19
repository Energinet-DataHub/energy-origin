using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class ValueObjectTests
{
    private class TestValueObject(string testProperty) : ValueObject
    {
        public string TestProperty { get; } = testProperty;
    }

    [Fact]
    public void ValueObject_Equality_Returns_True_When_Properties_Are_Equal()
    {
        var valueObject1 = new TestValueObject("Test");
        var valueObject2 = new TestValueObject("Test");

        (valueObject1 == valueObject2).Should().BeTrue();
        valueObject1.Equals(valueObject2).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_Equality_Returns_False_When_Properties_Are_Not_Equal()
    {
        var valueObject1 = new TestValueObject("Test1");
        var valueObject2 = new TestValueObject("Test2");

        (valueObject1 == valueObject2).Should().BeFalse();
        valueObject1.Equals(valueObject2).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_HashCode_Is_Consistent_For_Equal_Instances()
    {
        var valueObject1 = new TestValueObject("Test");
        var valueObject2 = new TestValueObject("Test");

        valueObject1.GetHashCode().Should().Be(valueObject2.GetHashCode());
    }

    [Fact]
    public void ValueObject_HashCode_Is_Different_For_Different_Instances()
    {
        var valueObject1 = new TestValueObject("Test1");
        var valueObject2 = new TestValueObject("Test2");

        valueObject1.GetHashCode().Should().NotBe(valueObject2.GetHashCode());
    }
}
