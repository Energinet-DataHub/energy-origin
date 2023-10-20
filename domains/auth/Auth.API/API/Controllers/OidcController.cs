using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
namespace API.Controllers;

[ApiController]
public class OidcController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [Route("auth/oidc/callback")]
    public async Task<IActionResult> CallbackAsync(
        IMetrics metrics,
        IDiscoveryCache discoveryCache,
        IHttpClientFactory clientFactory,
        IUserProviderService userProviderService,
        IUserService userService,
        ICryptography cryptography,
        ITokenIssuer issuer,
        OidcOptions oidcOptions,
        IdentityProviderOptions providerOptions,
        RoleOptions roleOptions,
        ILogger<OidcController> logger,
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        [FromQuery] string? state = default)
    {
        try
        {
            var oidcState = OidcState.Decode(state);

            var redirectionUri = OidcHelper.BuildRedirectionUri(oidcOptions, oidcState);

            var discoveryDocument = await discoveryCache.GetAsync();

            OidcHelper.TryVerifyCode(code, logger, error, errorDescription, redirectionUri);

            OidcHelper.TryVerifyDiscoveryDocument(discoveryDocument, logger, redirectionUri);

            var response = await OidcHelper.FetchTokenResponse(clientFactory, logger, oidcOptions, discoveryDocument, code!, redirectionUri);

            var (descriptor, data) = await OidcHelper.BuildUserDescriptor(logger, cryptography, userProviderService, userService, providerOptions, oidcOptions, roleOptions, discoveryDocument, response, redirectionUri);

            var token = issuer.Issue(descriptor, data);

            logger.AuditLog(
                "{User} created token for {Subject} at {TimeStamp}.",
                descriptor.Id,
                descriptor.Subject,
                DateTimeOffset.Now.ToUnixTimeSeconds()
            );

            metrics.Login(descriptor.Id, descriptor.Organization?.Id, descriptor.ProviderType);

            return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, "token", token));
        }
        catch (RedirectionFlow redirectionUrl)
        {
            return RedirectPreserveMethod(redirectionUrl.Url);
        }
    }

}
