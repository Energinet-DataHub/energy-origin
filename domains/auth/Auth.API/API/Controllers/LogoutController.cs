using API.Models.Dtos.Responses;
using API.Options;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class LogoutController : ControllerBase
{
    [HttpGet]
    [Route("auth/logout")]
    public async Task<IActionResult> LogoutAsync(IMetrics metrics, IDiscoveryCache discoveryCache,
        ICryptography cryptography, OidcOptions oidcOptions, ILogger<LogoutController> logger,
        [FromQuery] string? overrideRedirectionUri = default)
    {
        var redirectionUri = oidcOptions.FrontendRedirectUri.AbsoluteUri;
        if (oidcOptions.AllowRedirection && overrideRedirectionUri != null)
        {
            redirectionUri = overrideRedirectionUri;
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return Ok(new RedirectUriResponse { RedirectionUri = redirectionUri });
        }

        // trigger
        DecodableUserDescriptor descriptor;
        try
        {
            descriptor = new DecodableUserDescriptor(User, cryptography);
        }
        catch
        {
            return Ok(new RedirectUriResponse { RedirectionUri = redirectionUri });
        }

        var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint!);

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: descriptor.IdentityToken,
            postLogoutRedirectUri: redirectionUri
        );

        logger.AuditLog(
            "{User} logged off for {Subject} at {TimeStamp}.",
            descriptor.Id,
            descriptor.Subject,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        metrics.Logout(descriptor.Id, descriptor.Organization?.Id, descriptor.ProviderType);
        return Ok(new RedirectUriResponse { RedirectionUri = url });
    }
}
