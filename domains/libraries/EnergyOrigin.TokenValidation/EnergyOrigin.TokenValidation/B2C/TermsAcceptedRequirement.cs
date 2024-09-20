using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace EnergyOrigin.TokenValidation.b2c;

public class TermsAcceptedRequirement : IAuthorizationRequirement;

public class TermsAcceptedRequirementHandler : AuthorizationHandler<TermsAcceptedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TermsAcceptedRequirement requirement)
    {
        if (IsSubTypeUser(context))
        {
            if (IsTermsAccepted(context))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        else
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsSubTypeUser(AuthorizationHandlerContext context)
    {
        return ServiceCollectionExtensions.SubTypeUserClaimValues.Any(claimValue => context.User.HasClaim(ClaimType.SubType, claimValue));
    }

    private bool IsTermsAccepted(AuthorizationHandlerContext context)
    {
        return ServiceCollectionExtensions.BooleanTrueClaimValues.Any(claimValue => context.User.HasClaim(ClaimType.TermsAccepted, claimValue));
    }
}
