using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Utilities;

namespace Proxy;

public class TokenModificationMiddleware(RequestDelegate next, TokenSigner tokenSigner)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/wallet-api/v1") && context.Request.Query.ContainsKey("organizationId"))
        {
            var organizationId = context.Request.Query["organizationId"];
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var claims = jwtToken.Claims.ToList();
                var subClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier);

                if (subClaim!= null)
                {
                    claims.Remove(subClaim);
                    claims.Add(new Claim(JwtRegisteredClaimNames.Sub, organizationId!));
                }

                var newTokenString = tokenSigner.Sign(
                    subject: organizationId,
                    name: jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value?? "",
                    issuer: jwtToken.Issuer,
                    audience: jwtToken.Audiences.FirstOrDefault()?? "",
                    issueAt: jwtToken.ValidFrom,
                    duration: (int)(jwtToken.ValidTo - jwtToken.ValidFrom).TotalSeconds,
                    claims: claims.ToDictionary(c => c.Type, c => (object)c.Value)
                );

                context.Request.Headers["Authorization"] = $"Bearer {newTokenString}";
            }
        }

        await next(context);
    }
}
