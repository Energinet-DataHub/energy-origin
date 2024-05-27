using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace AccessControl.IntegrationTests.Setup;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var authorizationHeader = Context.Request.Headers[HeaderNames.Authorization].ToString().Substring(7);
        var securityToken = jwtSecurityTokenHandler.ReadToken(authorizationHeader) as JwtSecurityToken;

        if (!securityToken!.Claims.Any(c => c.Type == ClaimType.OrgIds))
        {
            return Task.FromResult(AuthenticateResult.Fail("Not authenticated"));
        }

        var identity = new ClaimsIdentity(securityToken!.Claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}
