using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace API.IntegrationTests.Setup;

public class EntraAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string AllowedClientId = "d216b90b-3872-498a-bc18-4941a0f4398e";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Context.Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.Fail("No Bearer token provided"));
        }

        var bearer = authHeader.Substring("Bearer ".Length).Trim();
        JwtSecurityToken jwtToken;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var tokenRead = handler.ReadToken(bearer);
            if (tokenRead is not JwtSecurityToken token)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
            }
            jwtToken = token;
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
        }

        var appIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "appid");
        if (appIdClaim == null || appIdClaim.Value != AllowedClientId)
        {
            return Task.FromResult(AuthenticateResult.Fail("Token not issued via Entra or invalid client"));
        }

        var identity = new ClaimsIdentity(jwtToken.Claims, "TestEntra");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
