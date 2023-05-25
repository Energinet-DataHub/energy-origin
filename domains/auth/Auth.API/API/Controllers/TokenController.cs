using System.Security.Claims;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TokenController : ControllerBase
{
    [HttpGet()]
    [Route("auth/token")]
    public async Task<IActionResult> RefreshAsync(
    	Metrics metrics,
        ILogger<TokenController> logger,
        IUserDescriptorMapper mapper,
        IUserService userService,
        ITokenIssuer tokenIssuer)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var versionBypass = false;

        var user = await userService.GetUserByIdAsync(descriptor.Id);

        if (user != null)
        {
            descriptor = mapper.Map(user, descriptor.ProviderType, descriptor.AccessToken!, descriptor.IdentityToken!);

            var scope = User.FindFirstValue(UserClaimName.Scope);

            if (scope!.Contains(UserScopeClaim.NotAcceptedTerms) == false) versionBypass = true;
        }

        var now = DateTimeOffset.Now;
        var token = tokenIssuer.Issue(descriptor, versionBypass, issueAt: now.UtcDateTime);

        logger.AuditLog(
            "{User} updated token for {Subject} at {TimeStamp}.",
            descriptor.Id,
            descriptor.Subject,
            now.ToUnixTimeSeconds()
        );

        metrics.TokenRefreshCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", descriptor?.Id),
            new KeyValuePair<string, object?>("CompanyId", descriptor?.CompanyId)
        );

        return Ok(token);
    }
}
