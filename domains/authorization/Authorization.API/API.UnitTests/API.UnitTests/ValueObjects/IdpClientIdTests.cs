using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class IdpClientIdTests
{
    [Fact]
    public void IdpClientId_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => new IdpClientId(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be an empty Guid. (Parameter 'value')");
    }

    [Fact]
    public void IdpClientId_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var validGuid = Guid.NewGuid();
        var idpClientId = new IdpClientId(validGuid);

        idpClientId.Value.Should().Be(validGuid);
    }

    [Fact]
    public void IdpClientId_Equality_Returns_True_When_Values_Are_Equal()
    {
        var guid = Guid.NewGuid();
        var idpClientId1 = new IdpClientId(guid);
        var idpClientId2 = new IdpClientId(guid);

        (idpClientId1 == idpClientId2).Should().BeTrue();
        idpClientId1.Equals(idpClientId2).Should().BeTrue();
    }

    [Fact]
    public void IdpClientId_Equality_Returns_False_When_Values_Are_Not_Equal()
    {
        var idpClientId1 = new IdpClientId(Guid.NewGuid());
        var idpClientId2 = new IdpClientId(Guid.NewGuid());

        (idpClientId1 == idpClientId2).Should().BeFalse();
        idpClientId1.Equals(idpClientId2).Should().BeFalse();
    }

    [Fact]
    public void IdpClientId_HashCode_Is_Consistent_For_Equal_Instances()
    {
        var guid = Guid.NewGuid();
        var idpClientId1 = new IdpClientId(guid);
        var idpClientId2 = new IdpClientId(guid);

        idpClientId1.GetHashCode().Should().Be(idpClientId2.GetHashCode());
    }

    [Fact]
    public void IdpClientId_HashCode_Is_Different_For_Different_Instances()
    {
        var idpClientId1 = new IdpClientId(Guid.NewGuid());
        var idpClientId2 = new IdpClientId(Guid.NewGuid());

        idpClientId1.GetHashCode().Should().NotBe(idpClientId2.GetHashCode());
    }
}
