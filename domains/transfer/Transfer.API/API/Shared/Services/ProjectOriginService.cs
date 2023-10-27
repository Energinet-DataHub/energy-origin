using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System;

namespace API.Shared.Services;

public abstract class ProjectOriginService
{
    protected static Metadata SetupDummyAuthorizationHeader(string owner)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", owner) }),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new Metadata
        {
            { "Authorization", $"Bearer {tokenHandler.WriteToken(token)}" }
        };
    }
}
