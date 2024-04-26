using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class IdpIdTests
{
    [Fact]
    public void IdpId_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => new IdpId(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be an empty Guid. (Parameter 'value')");
    }

    [Fact]
    public void IdpId_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var validGuid = Guid.NewGuid();
        var idpId = new IdpId(validGuid);

        idpId.Value.Should().Be(validGuid);
    }

    [Fact]
    public void IdpId_Equality_Returns_True_When_Values_Are_Equal()
    {
        var guid = Guid.NewGuid();
        var idpId1 = new IdpId(guid);
        var idpId2 = new IdpId(guid);

        (idpId1 == idpId2).Should().BeTrue();
        idpId1.Equals(idpId2).Should().BeTrue();
    }

    [Fact]
    public void IdpId_Equality_Returns_False_When_Values_Are_Not_Equal()
    {
        var idpId1 = new IdpId(Guid.NewGuid());
        var idpId2 = new IdpId(Guid.NewGuid());

        (idpId1 == idpId2).Should().BeFalse();
        idpId1.Equals(idpId2).Should().BeFalse();
    }

    [Fact]
    public void IdpId_HashCode_Is_Consistent_For_Equal_Instances()
    {
        var guid = Guid.NewGuid();
        var idpId1 = new IdpId(guid);
        var idpId2 = new IdpId(guid);

        idpId1.GetHashCode().Should().Be(idpId2.GetHashCode());
    }

    [Fact]
    public void IdpId_HashCode_Is_Different_For_Different_Instances()
    {
        var idpId1 = new IdpId(Guid.NewGuid());
        var idpId2 = new IdpId(Guid.NewGuid());

        idpId1.GetHashCode().Should().NotBe(idpId2.GetHashCode());
    }
}
