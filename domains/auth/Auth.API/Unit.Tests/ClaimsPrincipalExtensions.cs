using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Unit.Tests;

public static class TestClaimsPrincipal
{
    public static ClaimsPrincipal Make(Guid? id = default)
    {
        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, "descriptor.Name"),
            new(JwtRegisteredClaimNames.Sub, "descriptor.Subject.ToString()")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(identity));
    }
}
