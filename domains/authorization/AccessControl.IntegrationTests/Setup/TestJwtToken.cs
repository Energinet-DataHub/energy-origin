using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AccessControl.IntegrationTests.Setup;

public class TestJwtToken
{
    public List<Claim> Claims { get; } = new();
    public int ExpiresInMinutes { get; set; } = 30;

    public TestJwtToken WithRole(string roleName)
    {
        Claims.Add(new Claim(ClaimTypes.Role, roleName));
        return this;
    }

    public TestJwtToken WithUserName(string username)
    {
        Claims.Add(new Claim(ClaimTypes.Upn, username));
        return this;
    }

    public TestJwtToken WithEmail(string email)
    {
        Claims.Add(new Claim(ClaimTypes.Email, email));
        return this;
    }

    public TestJwtToken WithDepartment(string department)
    {
        Claims.Add(new Claim("department", department));
        return this;
    }

    public TestJwtToken WithExpiration(int expiresInMinutes)
    {
        ExpiresInMinutes = expiresInMinutes;
        return this;
    }

    public string Build()
    {
        var token = new JwtSecurityToken(
            JwtTokenProvider.Issuer,
            JwtTokenProvider.Issuer,
            Claims,
            expires: DateTime.Now.AddMinutes(ExpiresInMinutes),
            signingCredentials: JwtTokenProvider.SigningCredentials
        );
        return JwtTokenProvider.JwtSecurityTokenHandler.WriteToken(token);
    }
}
