using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
public class OidcController : ControllerBase
{
    [HttpGet()]
    [Route("auth/oidc/callback")]
    public async Task<IActionResult> CallbackAsync(
        IDiscoveryCache discoveryCache,
        IHttpClientFactory clientFactory,
        IUserDescriptMapper mapper,
        IUserService service,
        ITokenIssuer issuer,
        IOptions<OidcOptions> oidcOptions,
        ILogger<OidcController> logger,
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        [FromQuery] string? state = default)
    {
        var oidcState = OidcState.Decode(state);
        var redirectionUri = oidcOptions.Value.FrontendRedirectUri.AbsoluteUri;
        if (oidcOptions.Value.AllowRedirection && oidcState?.RedirectionUri != null)
        {
            redirectionUri = oidcState!.RedirectionUri;
        }

        if (code == null)
        {
            logger.LogWarning("Callback error: {error} - description: {errorDescription}", error, errorDescription);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.From(error, errorDescription)));
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }

        var client = clientFactory.CreateClient();
        var request = new AuthorizationCodeTokenRequest
        {
            Address = discoveryDocument.TokenEndpoint,
            Code = code,
            ClientId = oidcOptions.Value.ClientId,
            ClientSecret = oidcOptions.Value.ClientSecret,
            RedirectUri = oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri
        };
        var response = await client.RequestAuthorizationCodeTokenAsync(request);
        if (response.IsError)
        {
            request.ClientSecret = "<removed>";
            logger.LogError(response.Exception, "Failed in acquiring token with request details: {@request}", request);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.BadResponse));
        }

        string token;
        try
        {
            var descriptor = await MapUserDescriptor(mapper, service, oidcOptions.Value, discoveryDocument, response);
            token = issuer.Issue(descriptor);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.Authentication.InvalidTokens)
            );
            return RedirectPreserveMethod(url);
        }

        if (oidcState?.State != null)
        {
            redirectionUri = QueryHelpers.AddQueryString(redirectionUri, "state", oidcState!.State);
        }

        return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, "token", token));
    }

    private static async Task<UserDescriptor> MapUserDescriptor(IUserDescriptMapper mapper, IUserService service, OidcOptions oidcOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response)
    {
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };
        var parameters = new TokenValidationParameters()
        {
            IssuerSigningKeys = discoveryDocument.KeySet.Keys.Select(it => it.ToSecurityKey()),
            ValidIssuer = discoveryDocument.Issuer,
            ValidAlgorithms = discoveryDocument.TryGetStringArray("request_object_signing_alg_values_supported"),
            ValidAudience = oidcOptions.ClientId
        };

        var userInfo = handler.ValidateToken(response.TryGet("userinfo_token"), parameters, out _);
        var identity = handler.ValidateToken(response.IdentityToken, parameters, out _);

        parameters.ValidateAudience = false;
        var access = handler.ValidateToken(response.AccessToken, parameters, out _);

        var subject = access.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var scope = access.FindFirstValue("scope");
        var providerId = userInfo.FindFirstValue("mitid.uuid");
        var name = userInfo.FindFirstValue("mitid.identity_name");
        var tin = userInfo.FindFirstValue("nemid.cvr");

        ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrEmpty(scope, nameof(scope));
        ArgumentException.ThrowIfNullOrEmpty(providerId, nameof(providerId));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        if (subject != identity.FindFirstValue(JwtRegisteredClaimNames.Sub) || subject != userInfo.FindFirstValue(JwtRegisteredClaimNames.Sub))
        {
            throw new SecurityTokenException("Subject mismatched found in tokens received.");
        }

        var user = await service.GetUserByProviderIdAsync(providerId) ?? new User
        {
            Id = null,
            ProviderId = providerId,
            Name = name,
            Tin = tin,
            AcceptedTermsVersion = 0,
            AllowCPRLookup = false
        };
        return mapper.Map(user, response.AccessToken, response.IdentityToken);
    }
}
