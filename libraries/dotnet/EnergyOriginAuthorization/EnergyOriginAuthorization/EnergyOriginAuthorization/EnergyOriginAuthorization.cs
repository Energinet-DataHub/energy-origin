using EnergyOriginAuthorization.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace EnergyOriginAuthorization
{
    public class Authorization
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class AuthorizeAttribute : Attribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (Configuration.IsDevelopment())
                {
                    return;
                }

                var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last() ?? "";

                if (!ValidateToken(token))
                {
                    context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                }
            }

            public bool ValidateToken(string token)
            {
                //Note - Key exchange has not yet been implemented so we do not yet validate the issuer signing.
                //See https://jasonwatmore.com/post/2022/01/19/net-6-create-and-validate-jwt-tokens-use-custom-jwt-middleware

                var tokenHandler = new JwtSecurityTokenHandler();

                try
                {
                    var jwt = tokenHandler.ReadJwtToken(token);

                    var actor = jwt.Payload.First(x => x.Key == "actor").Value;

                    return !string.IsNullOrWhiteSpace(actor.ToString()) &&
                        actor.GetType() == typeof(string);
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
