using System.Security.Claims;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using static API.Utilities.AuthorizePolicies.RolesPolicy;

namespace API.Utilities.AuthorizePolicies;
public class RolesPolicy : AuthorizationHandler<Requirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Requirement requirement)
    {
        var matches = new[] {
            context.User.FindFirstValue(UserClaimName.MatchedRoles), context.User.FindFirstValue(UserClaimName.AssignedRoles)
            }.OfType<string>()
            .Any(x => x.Split(" ").Any(x => requirement.RequiredRoles.Contains(x)));

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
        public IEnumerable<string> RequiredRoles { get; }

        public Requirement(IEnumerable<string> requiredRoles) => RequiredRoles = requiredRoles;
    }
}
