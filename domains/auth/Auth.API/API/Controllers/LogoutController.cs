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
    // [HttpGet()]
    // [Route("auth/logout")]
    // public async Task<IActionResult> LogoutAsync(IMetrics metrics, IDiscoveryCache discoveryCache, ICryptography cryptography, OidcOptions oidcOptions, ILogger<LogoutController> logger, [FromQuery] string? overrideRedirectionUri = default)
    // {
    //     var redirectionUri = oidcOptions.FrontendRedirectUri.AbsoluteUri;
    //     if (oidcOptions.AllowRedirection && overrideRedirectionUri != null)
    //     {
    //         redirectionUri = overrideRedirectionUri;
    //     }
    //
    //     var discoveryDocument = await discoveryCache.GetAsync();
    //     if (discoveryDocument == null || discoveryDocument.IsError)
    //     {
    //         logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
    //         return RedirectPreserveMethod(redirectionUri);
    //     }
    //
    //     var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint);
    //
    //     DecodableUserDescriptor descriptor;
    //     try
    //     {
    //         descriptor = new DecodableUserDescriptor(User, cryptography);
    //     }
    //     catch
    //     {
    //         return RedirectPreserveMethod(redirectionUri);
    //     }
    //
    //     var url = requestUrl.CreateEndSessionUrl(
    //         idTokenHint: descriptor.IdentityToken,
    //         postLogoutRedirectUri: redirectionUri
    //     );
    //
    //
    //     logger.AuditLog(
    //         "{User} logged off for {Subject} at {TimeStamp}.",
    //         descriptor.Id,
    //         descriptor.Subject,
    //         DateTimeOffset.Now.ToUnixTimeSeconds()
    //     );
    //
    //     metrics.Logout(descriptor.Id, descriptor.Organization?.Id, descriptor.ProviderType);
    //
    //     return RedirectPreserveMethod(url);
    // }


    [HttpGet()]
    [Route("auth/logout")]
    public async Task<IActionResult> LogoutAsync(IMetrics metrics, IDiscoveryCache discoveryCache, ICryptography cryptography, OidcOptions oidcOptions, ILogger<LogoutController> logger, [FromQuery] string? overrideRedirectionUri = default)
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
            return RedirectPreserveMethod(redirectionUri);
        }

        DecodableUserDescriptor descriptor;
        try
        {
            descriptor = new DecodableUserDescriptor(User, cryptography);
        }
        catch
        {
            return RedirectPreserveMethod(redirectionUri);
        }

        var client = new HttpClient();
        client.BaseAddress = new Uri(discoveryDocument.EndSessionEndpoint);

        var response = await client.GetAsync($"?id_token_hint={descriptor.IdentityToken}");

        if (response.IsSuccessStatusCode)
        {
            logger.AuditLog(
                "{User} logged off for {Subject} at {TimeStamp}.",
                descriptor.Id,
                descriptor.Subject,
                DateTimeOffset.Now.ToUnixTimeSeconds()
            );

            metrics.Logout(descriptor.Id, descriptor.Organization?.Id, descriptor.ProviderType);

            return Ok("Logout successful");
        }
        else
        {
            logger.LogError("Logout failed: {Error}", response.ReasonPhrase);
            return BadRequest("Logout failed");
        }
    }

}
