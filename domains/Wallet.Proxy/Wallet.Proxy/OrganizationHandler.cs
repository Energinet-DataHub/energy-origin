using Microsoft.AspNetCore.Authorization;

namespace Wallet.Proxy;

public class OrganizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OrganizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OrganizationRequirement requirement)
    {
        var orgIdQueryParam = httpContextAccessor.HttpContext?.Request.Query["organizationId"].FirstOrDefault();
        if (string.IsNullOrEmpty(orgIdQueryParam))
            return FailAuthorization(context);

        var orgIdsClaim = context.User.FindFirst("org_ids");
        if (orgIdsClaim == null || string.IsNullOrEmpty(orgIdsClaim.Value))
            return FailAuthorization(context);

        var listOfOrgIdsInsideClaim = System.Text.Json.JsonSerializer.Deserialize<List<string>>(orgIdsClaim.Value);
        if (listOfOrgIdsInsideClaim?.Contains(orgIdQueryParam) != true)
            return FailAuthorization(context);

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
    
    private static Task FailAuthorization(AuthorizationHandlerContext context)
    {
        context.Fail();
        return Task.CompletedTask;
    }
}

public class OrganizationRequirement : IAuthorizationRequirement;
