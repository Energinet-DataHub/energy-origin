using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Unit.Tests;

public static class TestClaimsPrincipal
{
    public static void SetUser(this ControllerBase controller, Guid? id = default)
    {
        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, "descriptor.Name"),
            new(JwtRegisteredClaimNames.Sub, "descriptor.Subject.ToString()"),
            new(UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString()),
            // new(UserClaimName.AllowCprLookup, "true"),
            new(UserClaimName.Actor, id?.ToString() ?? Guid.NewGuid().ToString()),
            new(UserClaimName.MatchedRoles, ""),
            // new(UserClaimName.CompanyId, ""),
            // new(UserClaimName.CompanyName, ""),
            // new(UserClaimName.Tin, "")
        };

        controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(identity))
            }
        };
    }
}
