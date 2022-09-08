using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EnergyOriginAuthorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly IEnumerable<string> requiredScopes;
        public AuthorizationContext? Context { get; private set; }

        public AuthorizeAttribute() => requiredScopes = new List<string>();

        public AuthorizeAttribute(string requiredScope) => requiredScopes = new List<string>() { requiredScope };

        public AuthorizeAttribute(IEnumerable<string> requiredScopes) => this.requiredScopes = requiredScopes;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last() ?? "";

            if (!ValidateToken(token, requiredScopes))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Unauthorized" });
            }
        }

        public static bool ValidateToken(string token, IEnumerable<string> requiredScopes) => AuthorizationContext.decode(token, requiredScopes) != null;
    }
}
