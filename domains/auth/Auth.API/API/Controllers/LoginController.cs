using System.Diagnostics.Metrics;
using API.Options;
using API.Utilities;
using API.Values;
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
    public async Task<IActionResult> TestMethod()
    {
        Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
        Counter<int> s_hatsSold = s_meter.CreateCounter<int>(name: "hats-sold",
            unit: "Hats",
            description: "The number of hats sold in our store");

        s_hatsSold.Add(4);

        Histogram<int> lol = s_meter.CreateHistogram<int>("name", "unit", "description");

        lol.Record(111);

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
