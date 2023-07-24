using System.Security.Claims;
using API.Services.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;

namespace API.Utilities.AuthorizePolicies;

public class OrganizationOwnerPolicy: IAuthorizationHandler, IAuthorizationRequirement
{
    private readonly IUserService userService;
    public OrganizationOwnerPolicy(IUserService userService) => this.userService = userService;

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var rolesArray = context.User.FindFirstValue(UserClaimName.Roles)?.Split(' ');
        if (rolesArray?.Contains(RoleKeys.AuthAdminKey) == true)
        {
            context.Succeed(this);
            return Task.FromResult(0);
        }

        context.Fail();
        return Task.FromResult(0);
    }
}
