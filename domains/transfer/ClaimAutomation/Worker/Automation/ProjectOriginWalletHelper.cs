using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;

namespace ClaimAutomation.Worker.Automation;

public static class ProjectOriginWalletHelper
{
    public static Metadata SetupDummyAuthorizationHeader(string owner)
    {
        return new Metadata
        {
            { "Authorization", $"Bearer {GenerateBearerToken(owner)}" }
        };
    }

    public static string GenerateBearerToken(string owner)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", owner) }),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
