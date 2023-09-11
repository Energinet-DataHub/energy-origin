using System.Security.Claims;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.Utilities.TokenIssuer;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TokenController : ControllerBase
{
    [HttpGet()]
    [Route("auth/token")]
    public async Task<IActionResult> RefreshAsync(
        IMetrics metrics,
        ILogger<TokenController> logger,
        IUserService userService,
        ITokenIssuer tokenIssuer)
    {
        var descriptor = new UserDescriptor(User);
        var versionBypass = false;
        var user = await userService.GetUserByIdAsync(descriptor.Id);
        if (user != null)
        {
            var scope = User.FindFirstValue(UserClaimName.Scope);
            descriptor.Organization = new OrganizationDescriptor { Id = user.CompanyId, Name = user.Company.Name, Tin = user.Company.Tin }; // FIXME: this seems wrong
            if (scope!.Contains(UserScopeName.NotAcceptedPrivacyPolicy) == false) versionBypass = true;
        }

        var now = DateTimeOffset.Now;
        var token = tokenIssuer.Issue(descriptor, UserData.From(user), versionBypass, issueAt: now.UtcDateTime);

        logger.AuditLog(
            "{User} updated token for {Subject} at {TimeStamp}.",
            descriptor.Id,
            descriptor.Subject,
            now.ToUnixTimeSeconds()
        );

        metrics.TokenRefresh(descriptor.Id, descriptor.Organization?.Id, descriptor.ProviderType);

        return Ok(token);
    }
}
