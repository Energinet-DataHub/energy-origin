using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EnergyOriginAuthorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private IEnumerable<string> requiredScopes;
        public AuthorizationContext? Context { get; private set; }

        public AuthorizeAttribute()
        {
            requiredScopes = new List<string>();
        }

        public AuthorizeAttribute(string requiredScope)
        {
            requiredScopes = new List<string>() { requiredScope };
        }

        public AuthorizeAttribute(IEnumerable<string> requiredScopes)
        {
            requiredScopes = requiredScopes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last() ?? "";

            if (!ValidateToken(token, requiredScopes))
            {
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }

        public bool ValidateToken(string token, IEnumerable<string> requiredScopes)
        {
            return AuthorizationContext.decode(token, requiredScopes) != null;
        }
    }
}
