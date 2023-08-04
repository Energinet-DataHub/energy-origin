using System.Security.Claims;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;

namespace API.Utilities.AuthorizePolicies;

public class OrganizationOwnerPolicy : IAuthorizationHandler, IAuthorizationRequirement
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var rolesArray = context.User.FindFirstValue(UserClaimName.Roles)?.Split(' ');
        if (rolesArray?.Contains(RoleKeys.AuthAdminKey) == true)
        {
            context.Succeed(this);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
