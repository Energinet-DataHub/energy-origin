using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Proxy.IntegrationTests.Setup;

public class ProxyTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        if (!Context.Request.Headers.ContainsKey(HeaderNames.Authorization) ||
            string.IsNullOrWhiteSpace(Context.Request.Headers[HeaderNames.Authorization]))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authorizationHeader = Context.Request.Headers[HeaderNames.Authorization].ToString();
        if (!authorizationHeader.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
        }

        var token = authorizationHeader.Substring(7);

        try
        {
            var securityToken = jwtSecurityTokenHandler.ReadToken(token) as JwtSecurityToken;

            if (securityToken == null || !securityToken.Claims.Any(c => c.Type == ClaimType.OrgIds))
            {
                return Task.FromResult(AuthenticateResult.Fail("Not authenticated"));
            }

            var identity = new ClaimsIdentity(securityToken.Claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Invalid token: {ex.Message}"));
        }
    }
}
