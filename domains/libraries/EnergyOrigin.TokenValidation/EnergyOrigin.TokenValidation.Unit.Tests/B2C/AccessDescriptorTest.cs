using System.Security.Claims;
using EnergyOrigin.TokenValidation.b2c;
using FluentAssertions;

namespace EnergyOrigin.TokenValidation.Unit.Tests.B2C;

public class AccessDescriptorTest
{
    [Fact]
    public void GivenZeroedOrgIdClaim_WhenParsing_OrganizationIdIsParsed()
    {
        var claims = new List<Claim>
        {
            new(ClaimType.OrgId, "00000000-0000-0000-0000-000000000000"),
            new(ClaimType.SubType, "External"),
            new(ClaimType.OrgIds, "")
        };
        var contextAccessor = IdentityDescriptorTest.BuildContextAccessor(claims);
        var identityDescriptor = new IdentityDescriptor(contextAccessor);
        var sut = new AccessDescriptor(identityDescriptor);

        sut.IsAuthorizedToOrganization(Guid.Empty).Should().BeFalse();
    }

    [Fact]
    public void GivenAnExternalClient_WhenOrgIdIsPresent_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimType.OrgId, Guid.NewGuid().ToString()),
            new(ClaimType.SubType, "External")
        };

        var contextAccessor = IdentityDescriptorTest.BuildContextAccessor(claims);
        var identityDescriptor = new IdentityDescriptor(contextAccessor);
        var sut = new AccessDescriptor(identityDescriptor);

        // Act/Assert
        sut.IsExternalClientAuthorized().Should().BeTrue();
    }

    [Fact]
    public void GivenAnNonExternalClient_WhenOrgIdIsPresent_ShouldReturnFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimType.OrgId, Guid.NewGuid().ToString()),
            new(ClaimType.SubType, "Internal")
        };

        var contextAccessor = IdentityDescriptorTest.BuildContextAccessor(claims);
        var identityDescriptor = new IdentityDescriptor(contextAccessor);
        var sut = new AccessDescriptor(identityDescriptor);

        // Act/Assert
        sut.IsExternalClientAuthorized().Should().BeFalse();
    }

    [Fact]
    public void GivenAnExternalClient_WhenOrgIdIsNotPresent_ShouldReturnFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimType.OrgId, Guid.Empty.ToString()),
            new(ClaimType.SubType, "External")
        };

        var contextAccessor = IdentityDescriptorTest.BuildContextAccessor(claims);
        var identityDescriptor = new IdentityDescriptor(contextAccessor);
        var sut = new AccessDescriptor(identityDescriptor);

        // Act/Assert
        sut.IsExternalClientAuthorized().Should().BeFalse();
    }
}
