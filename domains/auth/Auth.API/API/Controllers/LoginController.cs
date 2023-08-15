using API.Options;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace API.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    [HttpGet()]
    [Route("auth/give-token")]
    public IActionResult RefreshAsync(
        ICryptography cryptography,
        ITokenIssuer tokenIssuer)
    {
        var descriptor = new UserDescriptor(cryptography)
        {
            Id = Guid.NewGuid(),
            Name = "Me",
            AllowCprLookup = true,
            ProviderType = ProviderType.NemIdProfessional,
            EncryptedAccessToken = "",
            EncryptedIdentityToken = "",
            MatchedRoles = RoleKey.Admin
        };
        var token = tokenIssuer.Issue(descriptor, new TokenIssuer.UserData(0, 0), true);
        return Ok(token);
    }

    [HttpGet]
    [Authorize(Roles = RoleKey.Admin)]
    [Route("auth/check-roles")]
    public IActionResult TestMethod3(RoleOptions options) => Ok(options);

    [HttpGet]
    [AllowAnonymous]
    [Route("auth/test")]
    public IActionResult TestMethod(TermsOptions termsOptions, ILogger<LoginController> logger)
    {
        logger.LogInformation("PrivacyVersion: {privacyVersion}", termsOptions.PrivacyPolicyVersion);
        logger.LogInformation("TosVersion: {tosVersion}", termsOptions.TermsOfServiceVersion);

        return Ok(new List<int>()
        {
            termsOptions.PrivacyPolicyVersion,
            termsOptions.TermsOfServiceVersion
        });
    }

    [HttpGet]
    [Route("auth/login")]
    public async Task<IActionResult> LoginAsync(IDiscoveryCache discoveryCache, OidcOptions oidcOptions, IdentityProviderOptions providerOptions, ILogger<LoginController> logger, [FromQuery] string? state = default, [FromQuery] string? overrideRedirectionUri = default)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }

        var (scope, arguments) = providerOptions.GetIdentityProviderArguments();

        var oidcState = new OidcState(
            State: state,
            RedirectionUri: overrideRedirectionUri
        );
        var requestUrl = new RequestUrl(discoveryDocument.AuthorizeEndpoint);
        var url = requestUrl.CreateAuthorizeUrl(
            clientId: oidcOptions.ClientId,
            responseType: "code",
            redirectUri: oidcOptions.AuthorityCallbackUri.AbsoluteUri,
            state: oidcState.Encode(),
            prompt: "login",
            scope: scope,
            extra: new Parameters(arguments));

        return RedirectPreserveMethod(url);
    }
}
