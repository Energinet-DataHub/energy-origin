using System.Diagnostics.Metrics;
using API.Options;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    [HttpGet()]
    [Route("auth/test")]
    public async Task<IActionResult> TestMethod(Metrics metrics)
    {
        metrics.LoginCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", "9d3c98f9-62c1-403c-b9a5-026a4f03580d"),
            new KeyValuePair<string, object?>("CompanyId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("IdentityProviderType", ProviderType.MitID_Professional)
        );

        metrics.LoginCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("CompanyId", "2d9c35f8-a580-4e6a-b98b-3d659ae979df"),
            new KeyValuePair<string, object?>("IdentityProviderType", ProviderType.NemID_Professional)
        );

        metrics.LoginCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("CompanyId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("IdentityProviderType", ProviderType.NemID_Professional)
        );

        metrics.LogoutCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("CompanyId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("IdentityProviderType", ProviderType.MitID_Professional)
        );

        metrics.TokenRefreshCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", Guid.NewGuid()),
            new KeyValuePair<string, object?>("CompanyId", Guid.NewGuid())
        );

        return Ok("Test message");
    }

    [HttpGet()]
    [Route("auth/login")]
    public async Task<IActionResult> LoginAsync(IDiscoveryCache discoveryCache, IOptions<OidcOptions> oidcOptions, IOptions<IdentityProviderOptions> providerOptions, ILogger<LoginController> logger, [FromQuery] string? state = default, [FromQuery] string? overrideRedirectionUri = default)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }

        var (scope, arguments) = providerOptions.Value.GetIdentityProviderArguments();

        var oidcState = new OidcState(
            State: state,
            RedirectionUri: overrideRedirectionUri
        );
        var requestUrl = new RequestUrl(discoveryDocument.AuthorizeEndpoint);
        var url = requestUrl.CreateAuthorizeUrl(
            clientId: oidcOptions.Value.ClientId,
            responseType: "code",
            redirectUri: oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri,
            state: oidcState.Encode(),
            prompt: "login",
            scope: scope,
            extra: new Parameters(arguments));

        return RedirectPreserveMethod(url);
    }
}
