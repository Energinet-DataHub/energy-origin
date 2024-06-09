using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace EnergyOrigin.TokenValidation.b2c;

public static class Policy
{
    public const string B2CPolicy = "B2C";
    public const string B2CCustomPolicyClientPolicy = "B2C-self";
    public const string B2CSubTypeUserPolicy = "subtype-user";
    public const string B2CCvrClaim = "cvr-claim";
}

public class IdentityMustHaveAccessToOrganizationRequirment : IAuthorizationRequirement
{
    public IdentityMustHaveAccessToOrganizationRequirment()
    {
    }
}

public class IdentityMustHaveAccessToOrganizationRequirmentHandler : AuthorizationHandler<IdentityMustHaveAccessToOrganizationRequirment>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityMustHaveAccessToOrganizationRequirmentHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IdentityMustHaveAccessToOrganizationRequirment requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        var requestedOrgId = httpContext.Request.Query["organizationId"].FirstOrDefault();

        if (HasAccessToRequestedOrgId(context, requestedOrgId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasAccessToRequestedOrgId(AuthorizationHandlerContext context, string? requestedOrgId)
    {
        var orgIds = context.User.Claims.Where(c => c.Type == ClaimType.OrgIds).FirstOrDefault().Value.Split(" ");
        return orgIds.Contains(requestedOrgId);
    }
}
