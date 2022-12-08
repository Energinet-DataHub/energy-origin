using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Oidc.Mock.Extensions;
using Oidc.Mock.Jwt;
using Oidc.Mock.Models;

namespace Oidc.Mock.Controllers;

public class AuthController : Controller
{
    private readonly Client client;
    private readonly User[] users;
    private readonly IJwtTokenGenerator tokenGenerator;
    private readonly ILogger<AuthController> logger;

    public AuthController(Client client, User[] users, IJwtTokenGenerator tokenGenerator, ILogger<AuthController> logger)
    {
        this.client = client;
        this.users = users;
        this.tokenGenerator = tokenGenerator;
        this.logger = logger;
    }

    [HttpPost]
    [Route("api/v1/session/logout")]
    public IActionResult LogOut() => Ok();

    [HttpGet]
    [Route("Connect/Authorize")]
    public IActionResult Authorize(string client_id, string redirect_uri)
    {
        var (isValid, validationError) = client.Validate(client_id, redirect_uri);
        if (!isValid)
        {
            return BadRequest(validationError);
        }

        var routeValues = new RouteValueDictionary();
        foreach (var keyValuePair in Request.Query)
        {
            routeValues.Add(keyValuePair.Key, keyValuePair.Value);
        }

        return RedirectToPage("/Connect/Signin", routeValues);
    }

    [HttpPost]
    [Route("Connect/Token")]
    public IActionResult Token(string grant_type, string code, string redirect_uri)
    {
        var authorizationHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
        logger.LogDebug($"connect/token: authorization header: {authorizationHeader.Scheme} {authorizationHeader.Parameter}");
        logger.LogDebug($"connect/token: form data: {string.Join("; ", Request.Form.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

        if (!string.Equals(grant_type, "authorization_code", StringComparison.InvariantCultureIgnoreCase))
        {
            return BadRequest($"Invalid grant_type. Must be 'authorization_code', but was '{grant_type}'");
        }

        var auth = (authorizationHeader.Parameter ?? ":").DecodeBase64();
        var split = auth.Split(":");
        var clientId = split[0];
        var clientSecret = split[1];

        var (isValid, validationError) = client.Validate(clientId, clientSecret, redirect_uri);
        if (!isValid)
        {
            logger.LogDebug($"connect/token: {validationError}");
            return BadRequest(validationError);
        }

        var user = users.FirstOrDefault(u => string.Equals(u.Name?.ToMd5(), code));
        if (user == null)
        {
            logger.LogDebug("connect/token: Invalid code - no matching user");
            return BadRequest("Invalid code - no matching user");
        }

        const int expirationInSeconds = 3600;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var baseClaims = new Dictionary<string, object>
        {
            { "iss", $"https://{Request.Host}/{Request.PathBase}" },
            { "iat", now },
            { "exp", now + expirationInSeconds }
        };

        return Ok(
            new
            {
                access_token = tokenGenerator.Generate(baseClaims),
                token_type = "Bearer",
                expires_in = expirationInSeconds,
                scope = "openid nemid mitid userinfo_token",
                id_token = tokenGenerator.Generate(baseClaims.Plus(user.IdToken)),
                userinfo_token = tokenGenerator.Generate(baseClaims.Plus(user.UserinfoToken))
            });
    }

    [HttpGet]
    [Route(".well-known/openid-configuration/jwks")]
    public IActionResult Jwks() =>
        Ok(new
        {
            keys = new[] { tokenGenerator.GetJwkProperties() }
        });
}
