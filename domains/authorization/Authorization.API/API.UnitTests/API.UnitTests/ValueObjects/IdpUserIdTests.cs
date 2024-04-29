using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class IdpUserIdTests
{
    [Fact]
    public void IdpUserId_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => new IdpUserId(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be an empty Guid. (Parameter 'value')");
    }

    [Fact]
    public void IdpUserId_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var validGuid = Guid.NewGuid();
        var idpUserId = new IdpUserId(validGuid);

        idpUserId.Value.Should().Be(validGuid);
    }

    [Fact]
    public void IdpUserId_Equality_Returns_True_When_Values_Are_Equal()
    {
        var guid = Guid.NewGuid();
        var idpUserId1 = new IdpUserId(guid);
        var idpUserId2 = new IdpUserId(guid);

        (idpUserId1 == idpUserId2).Should().BeTrue();
        idpUserId1.Equals(idpUserId2).Should().BeTrue();
    }

    [Fact]
    public void IdpUserId_Equality_Returns_False_When_Values_Are_Not_Equal()
    {
        var idpUserId1 = new IdpUserId(Guid.NewGuid());
        var idpUserId2 = new IdpUserId(Guid.NewGuid());

        (idpUserId1 == idpUserId2).Should().BeFalse();
        idpUserId1.Equals(idpUserId2).Should().BeFalse();
    }

    [Fact]
    public void IdpUserId_HashCode_Is_Consistent_For_Equal_Instances()
    {
        var guid = Guid.NewGuid();
        var idpUserId1 = new IdpUserId(guid);
        var idpUserId2 = new IdpUserId(guid);

        idpUserId1.GetHashCode().Should().Be(idpUserId2.GetHashCode());
    }

    [Fact]
    public void IdpUserId_HashCode_Is_Different_For_Different_Instances()
    {
        var idpUserId1 = new IdpUserId(Guid.NewGuid());
        var idpUserId2 = new IdpUserId(Guid.NewGuid());

        idpUserId1.GetHashCode().Should().NotBe(idpUserId2.GetHashCode());
    }
}
