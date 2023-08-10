using API.Options;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

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
            AcceptedPrivacyPolicyVersion = 1,
            AcceptedTermsOfServiceVersion = 1,
            AllowCprLookup = true,
            ProviderType = ProviderType.NemIdProfessional,
            EncryptedAccessToken = "",
            EncryptedIdentityToken = "",
            MatchedRoles = "",
            AssignedRoles = "Admin"
        };
        var token = tokenIssuer.Issue(descriptor, true);
        return Ok(token);
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [Route("auth/check-roles")]
    public IActionResult TestMethod3(IOptions<RoleOptions> options) => Ok(options.Value);

    [HttpGet]
    [AllowAnonymous]
    [Route("auth/roles")]
    public IActionResult TestMethod2(IOptions<RoleOptions> options) => Ok(options.Value);

    [HttpGet]
    [AllowAnonymous]
    [Route("auth/test")]
    public IActionResult TestMethod(IOptions<TermsOptions> termsOptions, ILogger<LoginController> logger)
    {
        logger.LogInformation("PrivacyVersion: {privacyVersion}", termsOptions.Value.PrivacyPolicyVersion);
        logger.LogInformation("TosVersion: {tosVersion}", termsOptions.Value.TermsOfServiceVersion);

        return Ok(new List<int>()
        {
            termsOptions.Value.PrivacyPolicyVersion,
            termsOptions.Value.TermsOfServiceVersion
        });
    }

    [HttpGet]
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
