using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class IdpOrganizationIdTests
{
    [Fact]
    public void IdpOrganizationId_Constructor_Throws_ArgumentException_When_Value_Is_Empty()
    {
        Action act = () => new IdpOrganizationId(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be an empty Guid. (Parameter 'value')");
    }

    [Fact]
    public void IdpOrganizationId_Constructor_Succeeds_When_Value_Is_Valid()
    {
        var validGuid = Guid.NewGuid();
        var idpOrganizationId = new IdpOrganizationId(validGuid);

        idpOrganizationId.Value.Should().Be(validGuid);
    }

    [Fact]
    public void IdpOrganizationId_Equality_Returns_True_When_Values_Are_Equal()
    {
        var guid = Guid.NewGuid();
        var idpOrganizationId1 = new IdpOrganizationId(guid);
        var idpOrganizationId2 = new IdpOrganizationId(guid);

        (idpOrganizationId1 == idpOrganizationId2).Should().BeTrue();
        idpOrganizationId1.Equals(idpOrganizationId2).Should().BeTrue();
    }

    [Fact]
    public void IdpOrganizationId_Equality_Returns_False_When_Values_Are_Not_Equal()
    {
        var idpOrganizationId1 = new IdpOrganizationId(Guid.NewGuid());
        var idpOrganizationId2 = new IdpOrganizationId(Guid.NewGuid());

        (idpOrganizationId1 == idpOrganizationId2).Should().BeFalse();
        idpOrganizationId1.Equals(idpOrganizationId2).Should().BeFalse();
    }

    [Fact]
    public void IdpOrganizationId_HashCode_Is_Consistent_For_Equal_Instances()
    {
        var guid = Guid.NewGuid();
        var idpOrganizationId1 = new IdpOrganizationId(guid);
        var idpOrganizationId2 = new IdpOrganizationId(guid);

        idpOrganizationId1.GetHashCode().Should().Be(idpOrganizationId2.GetHashCode());
    }

    [Fact]
    public void IdpOrganizationId_HashCode_Is_Different_For_Different_Instances()
    {
        var idpOrganizationId1 = new IdpOrganizationId(Guid.NewGuid());
        var idpOrganizationId2 = new IdpOrganizationId(Guid.NewGuid());

        idpOrganizationId1.GetHashCode().Should().NotBe(idpOrganizationId2.GetHashCode());
    }

}
