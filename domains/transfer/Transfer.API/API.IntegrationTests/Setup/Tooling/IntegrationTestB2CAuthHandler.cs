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

namespace API.IntegrationTests.Setup.Tooling;

public class IntegrationTestB2CAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public IntegrationTestB2CAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var authorizationHeader = Context.Request.Headers[HeaderNames.Authorization].ToString().Substring(7);
        var securityToken = jwtSecurityTokenHandler.ReadToken(authorizationHeader) as JwtSecurityToken;

        // Check for the existence of a claim that only B2C should provide
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
