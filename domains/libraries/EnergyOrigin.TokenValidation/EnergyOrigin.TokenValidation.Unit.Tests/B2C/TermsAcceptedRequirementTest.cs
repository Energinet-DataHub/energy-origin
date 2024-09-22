using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.b2c;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace EnergyOrigin.TokenValidation.Unit.Tests.B2C;

public class TermsAcceptedRequirementTest
{
    private readonly TermsAcceptedRequirementHandler _sut = new();

    [Fact]
    public async Task GivenUser_WhenTermsNotAccepted_Unauthorized()
    {
        var ctx = CreateContext(SubjectType.User.ToString(), false.ToString());
        await _sut.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task GivenUser_WhenTermsAccepted_Authorized()
    {
        var ctx = CreateContext(SubjectType.User.ToString(), true.ToString());
        await _sut.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Given3rdParty_NoTermsClaim_Authorized()
    {
        var ctx = CreateContext(SubjectType.External.ToString(), null);
        await _sut.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }

    private AuthorizationHandlerContext CreateContext(string subTypeValue, string? termsAcceptedValue)
    {
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimType.SubType, subTypeValue));
        if (termsAcceptedValue is not null)
        {
            claims.Add(new Claim(ClaimType.TermsAccepted, termsAcceptedValue));
        }

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext(new[] { new TermsAcceptedRequirement() }, principal, null);
    }
}
