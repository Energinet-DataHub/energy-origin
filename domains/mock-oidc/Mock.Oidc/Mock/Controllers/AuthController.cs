using Microsoft.AspNetCore.Mvc;
using Mock.Oidc.Extensions;
using Mock.Oidc.Jwt;
using Mock.Oidc.Models;

namespace Mock.Oidc.Controllers;

public class AuthController : Controller
{
    private readonly ClientDescriptor _client;
    private readonly UserDescriptor[] _users;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ClientDescriptor client, UserDescriptor[] users, IJwtTokenGenerator tokenGenerator, ILogger<AuthController> logger)
    {
        _client = client;
        _users = users;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    [HttpPost]
    [Route("Connect/Logout")]
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
    public IActionResult Token(string client_id, string code, string client_secret, string redirect_uri)
    {
        _logger.LogInformation($"connect/token: client_id={client_id} code={code} redirect_uri={redirect_uri}");
        _logger.LogInformation(string.Join("; ", Request.Form.Select(kvp => $"{kvp.Key}={kvp.Value}")));

        //var (isValid, validationError) = _client.Validate(client_id, client_secret, redirect_uri);
        //if (!isValid)
        //{
        //    _logger.LogInformation($"connect/token: {validationError}");
        //    return BadRequest(validationError);
        //}

        var user = _users.FirstOrDefault(u => string.Equals(u.Name?.ToMd5(), code));
        if (user == null)
        {
            _logger.LogInformation($"connect/token: Invalid code - no matching user");
            return BadRequest("Invalid code - no matching user");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var baseClaims = new Dictionary<string, object>
        {
            { "iss", $"{Request.Scheme}://{Request.Host}" },
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
}
