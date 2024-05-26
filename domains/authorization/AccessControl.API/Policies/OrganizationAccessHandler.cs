using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AccessControl.API.Policies;

public class OrganizationAccessHandler : AuthorizationHandler<OrganizationAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OrganizationAccessRequirement requirement)
    {
        var httpContext = (context.Resource as HttpContextAccessor)?.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var orgIdString = httpContext.Request.Query["organizationId"].ToString();
        if (!Guid.TryParse(orgIdString, out Guid organizationId) || organizationId == Guid.Empty)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var orgIdsClaim = context.User.Claims.FirstOrDefault(c => c.Type == "org_ids")?.Value;
        var orgIds = orgIdsClaim?.Split(' ') ?? [];
        if (orgIds.Contains(organizationId.ToString()))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

public class OrganizationAccessRequirement : IAuthorizationRequirement { }
