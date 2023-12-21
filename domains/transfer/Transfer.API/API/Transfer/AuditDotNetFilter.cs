using System;
using System.Threading.Tasks;
using Audit.Core;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace API.Transfer;

public class AuditDotNetFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actorId = Guid.Empty;
        var actorName = "system";
        var subjectId = Guid.Empty;

        if (context.HttpContext?.User != null)
        {
            var user = new UserDescriptor(context.HttpContext.User);
            actorId = user.Id;
            actorName = user.Name;
            subjectId = user.Subject;

        }

        Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
        {
            scope.Event.CustomFields["ActorId"] = actorId;
            scope.Event.CustomFields["ActorName"] = actorName;
            scope.Event.CustomFields["SubjectId"] = subjectId;
        });

        await next();
    }
}
