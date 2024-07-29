using System.Security.Claims;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.B2C;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace EnergyOrigin.TokenValidation.Unit.Tests.B2C;

public class TermsRequirementHandlerTests
{
    private readonly TermsRequirementHandler termsRequirementHandler = new();
    private readonly TermsRequirement termsRequirement = new();

    [Fact]
    public async Task GivenTermsCheckDisabledWithAnnotationInController_And_NoTermsAcceptedClaim_WhenAuthorizationIsChecked_ThenRequirementShouldSucceed()
    {
        var context = CreateContextWithDisabledTermsCheck(null);
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task GivenTermsCheckDisabledWithAnnotationInController_And_TosAcceptedClaimIsTrue_WhenAuthorizationIsChecked_ThenRequirementShouldSucceed()
    {
        var context = CreateContextWithDisabledTermsCheck("true");
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task GivenTermsCheckDisabledWithAnnotationInController_And_TosAcceptedClaimIsFalse_WhenAuthorizationIsChecked_ThenRequirementShouldSucceed()
    {
        var context = CreateContextWithDisabledTermsCheck("false");
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task GivenTermsCheckDisabledWithAnnotationInController_And_TosAcceptedClaimIsInvalid_WhenAuthorizationIsChecked_ThenRequirementShouldSucceed()
    {
        var context = CreateContextWithDisabledTermsCheck("invalid");
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task GivenTermsCheckEnabledByDefault_And_NoTosAcceptedClaim_WhenAuthorizationIsChecked_ThenRequirementShouldNotSucceed()
    {
        var context = CreateContextWithEnabledTermsCheck(null);
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task GivenTermsCheckEnabledByDefault_And_TosAcceptedClaimIsTrue_WhenAuthorizationIsChecked_ThenRequirementShouldSucceed()
    {
        var context = CreateContextWithEnabledTermsCheck("true");
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task GivenTermsCheckEnabledByDefault_And_TosAcceptedClaimIsFalse_WhenAuthorizationIsChecked_ThenRequirementShouldNotSucceed()
    {
        var context = CreateContextWithEnabledTermsCheck("false");
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task GivenTermsCheckEnabledByDefault_And_TosAcceptedClaimIsInvalid_WhenAuthorizationIsChecked_ThenRequirementShouldNotSucceed()
    {
        var context = CreateContextWithEnabledTermsCheck("invalid");
        await termsRequirementHandler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    private AuthorizationHandlerContext CreateContextWithDisabledTermsCheck(string? tosAcceptedValue)
    {
        var metadata = new EndpointMetadataCollection(new DisableTermsRequirementAttribute());
        var routePattern = RoutePatternFactory.Parse("/");
        var endpoint = new RouteEndpoint(
            requestDelegate: _ => Task.CompletedTask,
            routePattern: routePattern,
            order: 0,
            metadata: metadata,
            displayName: null);

        var claims = new List<Claim>();
        if (tosAcceptedValue != null)
        {
            claims.Add(new Claim(ClaimType.TosAccepted, tosAcceptedValue));
        }

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext(new[] { termsRequirement }, principal, endpoint);
    }

    private AuthorizationHandlerContext CreateContextWithEnabledTermsCheck(string? tosAcceptedValue)
    {
        var claims = new List<Claim>();
        if (tosAcceptedValue != null)
        {
            claims.Add(new Claim(ClaimType.TosAccepted, tosAcceptedValue));
        }

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext(new[] { termsRequirement }, principal, null);
    }
}
