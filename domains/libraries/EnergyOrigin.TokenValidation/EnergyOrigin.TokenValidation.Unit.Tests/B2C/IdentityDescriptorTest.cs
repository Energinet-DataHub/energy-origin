using System.Security.Claims;
using EnergyOrigin.TokenValidation.b2c;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;

namespace EnergyOrigin.TokenValidation.Unit.Tests.B2C;

public class IdentityDescriptorTest
{
    [Fact]
    public void GivenEmptyOrgIdsClaim_WhenParsing_AuthorizedOrgIdsIsEmpty()
    {
        var claims = new List<Claim> { new(ClaimType.OrgIds, "") };
        var contextAccessor = BuildContextAccessor(claims);
        var sut = new IdentityDescriptor(contextAccessor);

        sut.AuthorizedOrganizationIds.Should().BeEmpty();
    }

    [Fact]
    public void GivenSpacesInOrgIdsClaim_WhenParsing_AuthorizedOrgIdsIsEmpty()
    {
        var claims = new List<Claim> { new(ClaimType.OrgIds, "   ") };
        var contextAccessor = BuildContextAccessor(claims);
        var sut = new IdentityDescriptor(contextAccessor);

        sut.AuthorizedOrganizationIds.Should().BeEmpty();
    }

    [Fact]
    public void GivenInvalidGuidInOrgIdsClaim_WhenParsing_AuthorizedOrgIdsIsEmpty()
    {
        var claims = new List<Claim> { new(ClaimType.OrgIds, "-1234-") };
        var contextAccessor = BuildContextAccessor(claims);
        var sut = new IdentityDescriptor(contextAccessor);

        sut.AuthorizedOrganizationIds.Should().BeEmpty();
    }

    [Fact]
    public void GivenSingleGuidInOrgIdsClaim_WhenParsing_AuthorizedOrgIdsContainsGuid()
    {
        var orgId = Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimType.OrgIds, orgId.ToString()) };
        var contextAccessor = BuildContextAccessor(claims);
        var sut = new IdentityDescriptor(contextAccessor);

        sut.AuthorizedOrganizationIds.Should().ContainSingle();
        sut.AuthorizedOrganizationIds.First().Should().Be(orgId);
    }

    [Fact]
    public void GivenMultipleGuidsInOrgIdsClaim_WhenParsing_AuthorizedOrgIdsContainsGuids()
    {
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimType.OrgIds, $"{org1Id.ToString()} {org2Id.ToString()}") };
        var contextAccessor = BuildContextAccessor(claims);
        var sut = new IdentityDescriptor(contextAccessor);

        sut.AuthorizedOrganizationIds.Should().HaveCount(2);
        sut.AuthorizedOrganizationIds.Should().Contain(org1Id);
        sut.AuthorizedOrganizationIds.Should().Contain(org2Id);
    }

    [Fact]
    public void GivenEmptyOrgIdClaim_WhenParsing_ExceptionIsThrown()
    {
        var claims = new List<Claim> { new(ClaimType.OrgId, "") };
        var contextAccessor = BuildContextAccessor(claims);
        var sut = new IdentityDescriptor(contextAccessor);

        Assert.Throws<InvalidOperationException>(() => sut.OrganizationId);
    }

    public static HttpContextAccessor BuildContextAccessor(IEnumerable<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(new[] { claimsIdentity });
        var featureCollection = new FeatureCollection();
        var authenticationResult = Substitute.For<IAuthenticateResultFeature>();
        authenticationResult.AuthenticateResult.Returns(
            HandleRequestResult.Success(new AuthenticationTicket(principal, AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme)));
        featureCollection.Set(authenticationResult);
        var contextAccessor = new HttpContextAccessor() { HttpContext = new DefaultHttpContext(featureCollection) { User = principal } };
        return contextAccessor;
    }
}
