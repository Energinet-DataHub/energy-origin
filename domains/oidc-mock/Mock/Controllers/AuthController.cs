using Microsoft.AspNetCore.Mvc;
using Oidc.Mock.Extensions;
using Oidc.Mock.Jwt;
using Oidc.Mock.Models;
using System.Net.Http.Headers;

namespace Oidc.Mock.Controllers;

public class AuthController : Controller
{
    private readonly Client _client;
    private readonly User[] _users;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(Client client, User[] users, IJwtTokenGenerator tokenGenerator, ILogger<AuthController> logger)
    {
        _client = client;
        _users = users;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    [HttpPost]
    [Route("api/v1/session/logout")]
    public IActionResult LogOut() => Ok();

    [HttpGet]
    [Route("Connect/Authorize")]
    public IActionResult Authorize(string client_id, string redirect_uri)
    {
        var (isValid, validationError) = _client.Validate(client_id, redirect_uri);
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
        _logger.LogDebug($"connect/token: authorization header: {authorizationHeader.Scheme} {authorizationHeader.Parameter}");
        _logger.LogDebug($"connect/token: form data: {string.Join("; ", Request.Form.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

        if (!string.Equals(grant_type, "authorization_code", StringComparison.InvariantCultureIgnoreCase))
        {
            return BadRequest($"Invalid grant_type. Must be 'authorization_code', but was '{grant_type}'");
        }

        var auth = (authorizationHeader.Parameter ?? ":").DecodeBase64();
        var split = auth.Split(":");
        var clientId = split[0];
        var clientSecret = split[1];

        var (isValid, validationError) = _client.Validate(clientId, clientSecret, redirect_uri);
        if (!isValid)
        {
            _logger.LogDebug($"connect/token: {validationError}");
            return BadRequest(validationError);
        }

        var user = _users.FirstOrDefault(u => string.Equals(u.Name?.ToMd5(), code));
        if (user == null)
        {
            _logger.LogDebug("connect/token: Invalid code - no matching user");
            return BadRequest("Invalid code - no matching user");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var baseClaims = new Dictionary<string, object>
        {
            { "iss", $"https://{Request.Host}/{Request.PathBase}" },
            { "iat", now },
            { "exp", now + 3600 }
        };

        return Ok(
            new
            {
                access_token = _tokenGenerator.Generate(baseClaims),
                token_type = "Bearer",
                expires_in = 3600,
                scope = "openid nemid mitid userinfo_token",
                id_token = _tokenGenerator.Generate(baseClaims.Plus(user.IdToken)),
                userinfo_token = _tokenGenerator.Generate(baseClaims.Plus(user.UserinfoToken))
            });
    }

    [HttpGet]
    [Route(".well-known/openid-configuration/jwks")]
    public IActionResult Jwks() =>
        Ok(new
        {
            keys = new[] { _tokenGenerator.GetJwkProperties() }
        });
}
