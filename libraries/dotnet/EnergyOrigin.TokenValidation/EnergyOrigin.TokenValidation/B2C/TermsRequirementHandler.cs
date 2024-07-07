using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;

namespace EnergyOrigin.TokenValidation.B2C;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableTermsRequirementAttribute : Attribute;

public class TermsRequirement : IAuthorizationRequirement;

public class TermsRequirementHandler : AuthorizationHandler<TermsRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TermsRequirement requirement)
    {
        var endpoint = context.Resource as Microsoft.AspNetCore.Http.Endpoint;
        var disableTermsAttribute = endpoint?.Metadata.GetMetadata<DisableTermsRequirementAttribute>();

        if (disableTermsAttribute != null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(c => c is { Type: ClaimType.TosAccepted, Value: "true" }))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
