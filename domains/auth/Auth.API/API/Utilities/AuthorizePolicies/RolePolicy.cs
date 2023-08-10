using System.Security.Claims;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using static API.Utilities.AuthorizePolicies.RolePolicy;

namespace API.Utilities.AuthorizePolicies;
public class RolePolicy : AuthorizationHandler<Requirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Requirement requirement)
    {
        var matches = new[] {
            context.User.FindFirstValue(UserClaimName.MatchedRoles), context.User.FindFirstValue(UserClaimName.AssignedRoles)
            }.OfType<string>()
            .Any(x => x.Split(" ").Contains(requirement.RequiredRole));

        // FIXME: evaulate
        // if (matches)
        // {
        //     context.Succeed(requirement);
        // }
        // else
        // {
        //     context.Fail();
        // }

        (matches ? (Action)(() => context.Succeed(requirement)) : context.Fail)();

        return Task.CompletedTask;
    }

    public class Requirement : IAuthorizationRequirement
    {
        public string RequiredRole { get; }

        public Requirement(string requiredRole) => RequiredRole = requiredRole;
    }
}
