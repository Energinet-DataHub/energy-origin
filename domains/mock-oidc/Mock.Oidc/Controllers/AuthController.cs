using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Mvc;
using Mock.Oidc.Extensions;
using Mock.Oidc.Models;

namespace Mock.Oidc.Controllers;

public class AuthController : Controller
{
    private readonly ClientDescriptor _client;
    private readonly UserDescriptor[] _users;

    public AuthController(ClientDescriptor client, UserDescriptor[] users)
    {
        _client = client;
        _users = users;
    }

    [HttpPost]
    [Route("Connect/Logout")]
    public IActionResult LogOut() => Ok();

    [HttpPost]
    [Route("Connect/Token")]
    public IActionResult Token(string client_id, string code, string client_secret, string redirect_uri)
    {
        //TODO: Validate against _client
        
        var user = _users.FirstOrDefault(u => string.Equals(u.Name?.ToMd5(), code));

        if (user == null)
        {
            return BadRequest("Invalid code - no matching user");
        }

        var issuer = $"{Request.Scheme}://{Request.Host}";
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return Ok(
            new
            {
                access_token = GenerateToken(issuer, issuedAt, new()),
                token_type = "Bearer",
                expires_in = 3600,
                scope = "openid nemid mitid userinfo_token",
                id_token = GenerateToken(issuer, issuedAt, user.IdToken),
                userinfo_token = GenerateToken(issuer, issuedAt, user.UserinfoToken)
            });
    }

    private static string GenerateToken(string issuer, long issuedAt, Dictionary<string, object> claims)
    {
        var rsa = RSAProvider.RSA;

        return JwtBuilder.Create()
            .WithAlgorithm(new RS256Algorithm(rsa, rsa))
            .AddClaim("iss", issuer)
            .AddClaim("iat", issuedAt)
            .AddClaim("exp", issuedAt + 3600) // One hour until expiry
            .AddClaims(claims)
            .Encode();
    }
}