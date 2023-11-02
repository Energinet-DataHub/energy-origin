using System.Threading.Tasks;
using API.Shared.Extensions;
using Audit.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Transfer;

public class AuditDotNetFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var claimsPrincipal = httpContext.User;

        var actorId = claimsPrincipal.FindActorGuidClaim();
        var actorName = claimsPrincipal.FindActorNameClaim();
        var subjectId = claimsPrincipal.FindSubjectGuidClaim();

        Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
        {
            scope.Event.CustomFields["ActorId"] = actorId;
            scope.Event.CustomFields["ActorName"] = actorName;
            scope.Event.CustomFields["SubjectId"] = subjectId;
        });

        await next();
    }
}



