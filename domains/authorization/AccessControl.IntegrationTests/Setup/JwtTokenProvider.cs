using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AccessControl.IntegrationTests.Setup;

public static class JwtTokenProvider
{
    public static string Issuer { get; } = "Sample_Auth_Server";
    public static SecurityKey SecurityKey { get; } =
        new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes("This_is_a_super_secure_key_and_you_know_it")
        );
    public static SigningCredentials SigningCredentials { get; } =
        new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
    internal static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new();
}
