using API.Helpers;
using API.Models.Oidc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.services;
public class TokenHandler
{
    private byte[] internalToken = Encoding.ASCII.GetBytes(Configuration.GetInternalTokenSecret());
    public string EncodeJwtToken(AuthState state)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("state", state.ToString()) }),
            Expires = DateTime.UtcNow.AddDays(Configuration.GetTokenExpiryTimeInDays()),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(internalToken), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

// NOT IN USE YET
// Create Internal Token dataclass
/*
var internalToken = new InternalToken
{
    Issued = DateTime.Now,
    Expires = DateTime.Now.AddDays(Configuration.GetTokenExpiryTimeInDays()),
    Actor = "lol",
    Subject = "lolz",
    Scope = Configuration.GetScopes()
};
*/
