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
    [Route("Connect/Token")]
    public IActionResult Token(string client_id, string code, string client_secret, string redirect_uri)
    {
        //TODO: Validate against _client
        
        var user = _users.FirstOrDefault(u => string.Equals(u.Name?.ToMd5(), code));

        if (user == null)
        {
            return BadRequest("Invalid code - no matching user");
        }
        
        return Ok(
            new
            {
                access_token = GenerateToken(),
                token_type = "Bearer",
                expires_in = 3600,
                scope = "openid nemid mitid userinfo_token",
                id_token = GenerateToken(),
                userinfo_token = GenerateToken()
            });
    }

    private static string GenerateToken()
    {
        var rsa = RSAProvider.RSA;

        var token = JwtBuilder.Create()
            .WithAlgorithm(new RS256Algorithm(rsa, rsa))
            .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
            .AddClaim("claim1", 0)
            .AddClaim("claim2", "claim2-value")
            .Encode();

        return token;
    }
}