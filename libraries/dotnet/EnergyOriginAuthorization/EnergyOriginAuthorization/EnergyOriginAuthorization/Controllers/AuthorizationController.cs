using Microsoft.AspNetCore.Mvc;

namespace EnergyOriginAuthorization
{
    public abstract class AuthorizationController : ControllerBase
    {
        public AuthorizationContext Context
        {
            get
            {
                var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (token == null)
                {
                    throw new UnauthorizedAccessException("Access is denied by default, since no authorization header was found.");
                }

                var context = AuthorizationContext.decode(token);
                if (context == null)
                {
                    throw new UnauthorizedAccessException("Access is denied by default, since no authorization context was decoded.");
                }

                return context;
            }
        }
    }
}