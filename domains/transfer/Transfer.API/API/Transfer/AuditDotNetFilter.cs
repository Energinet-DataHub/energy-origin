using System.Threading.Tasks;
using Audit.Core;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Transfer;

public class AuditDotNetFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {

        var user = new UserDescriptor(context.HttpContext.User);

        Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
        {
            scope.Event.CustomFields["ActorId"] = user.Id;
            scope.Event.CustomFields["ActorName"] = user.Name;
            scope.Event.CustomFields["SubjectId"] = user.Subject;
        });

        await next();
    }
}
