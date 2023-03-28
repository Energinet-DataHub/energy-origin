using API.Options;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
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

        var (scope, arguments) = GetIdentityProviderArguments(providerOptions.Value);

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
            scope: scope,
            extra: new Parameters(arguments));

        return RedirectPreserveMethod(url);
    }

    private (string, List<KeyValuePair<string, string>>) GetIdentityProviderArguments(IdentityProviderOptions providerOptions)
    {
        var scope = "openid ssn userinfo_token";
        var idp_values = string.Empty;
        var idp_params = string.Empty;

        if (providerOptions.Providers.Contains(ProviderType.NemID_Private) || providerOptions.Providers.Contains(ProviderType.NemID_Professional))
        {
            scope = string.Join(" ", scope, "nemid"); // TODO: Is nemid.pid automatically included for nemid or does the scope need to be included?
            idp_values = "nemid";

            if (providerOptions.Providers.Contains(ProviderType.NemID_Private) && providerOptions.Providers.Contains(ProviderType.NemID_Professional))
            {
                scope = string.Join(" ", scope, "private_to_business", "nemid.pid");
                idp_params =
                    """
                    "nemid": {"amr_values": "nemid.otp nemid.keyfile"}
                    """;
            }
            else
            {
                idp_params = providerOptions.Providers.Contains(ProviderType.NemID_Private)
                    ? """
                    "nemid": {"amr_values": "nemid.otp"}
                    """
                    : """
                    "nemid": {"amr_values": "nemid.keyfile"}
                    """;
            }
        }

        if (providerOptions.Providers.Contains(ProviderType.MitID_Private) || providerOptions.Providers.Contains(ProviderType.MitID_Professional))
        {
            if (providerOptions.Providers.Contains(ProviderType.MitID_Private) && providerOptions.Providers.Contains(ProviderType.MitID_Professional))
            {
                idp_params = string.Join(idp_params.IsNullOrEmpty() ? null : ", ", idp_params,
                    """
                    "mitid_erhverv": {"allow_private":true}
                    """);
            }

            if (providerOptions.Providers.Contains(ProviderType.MitID_Private))
            {
                scope = string.Join(" ", scope, "mitid");
                idp_values = string.Join(idp_values.IsNullOrEmpty() ? null : " ", idp_values, "mitid");
            }

            if (providerOptions.Providers.Contains(ProviderType.MitID_Professional))
            {
                scope = string.Join(" ", scope, "nemlogin");
                idp_values = string.Join(idp_values.IsNullOrEmpty() ? null : " ", idp_values, "mitid_erhverv");
            }
        }

        idp_params = idp_params.IsNullOrEmpty() ? idp_params : $$"""{{{idp_params}}}""";

        var arguments = new List<KeyValuePair<string, string>>();

        if (idp_values.IsNullOrEmpty() == false)
        {
            arguments.Add(new KeyValuePair<string, string>("idp_values", idp_values));
        }

        if (idp_params.IsNullOrEmpty() == false)
        {
            arguments.Add(new KeyValuePair<string, string>("idp_params", idp_params));
        }

        return (scope, arguments);
    }
}
