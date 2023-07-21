using System.Security.Claims;
using API.Services.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;

namespace API.Utilities.AuthorizePolicies;

public class RoleAdminPolicy : IAuthorizationHandler, IAuthorizationRequirement
{
    private readonly IUserService userService;
    public RoleAdminPolicy(IUserService userService) => this.userService = userService;

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var user = await userService.GetUserByIdAsync(Guid.Parse(context.User.FindFirstValue(UserClaimName.Actor)!));
        if (user?.Roles.Any(x => x.RoleAdmin) ?? false)
        {
            context.Succeed(this);
        }
    }
}
