using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Utilities;
using API.Values;
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
        IHttpContextAccessor accessor,
        IDiscoveryCache discoveryCache,
        IHttpClientFactory clientFactory,
        IUserDescriptMapper mapper,
        IUserService service,
        ITokenIssuer issuer,
        IOptions<OidcOptions> oidcOptions,
        IOptions<TokenOptions> tokenOptions,
        ILogger<OidcController> logger,
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        if (code == null)
        {
            logger.LogWarning("Callback error: {error} - description: {errorDescription}", error, errorDescription);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", ErrorCodeFrom(error, errorDescription)));
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }

        var client = clientFactory.CreateClient();
        var request = new AuthorizationCodeTokenRequest
        {
            Address = discoveryDocument.TokenEndpoint,
            Code = code,
            ClientId = oidcOptions.Value.ClientId,
            ClientSecret = oidcOptions.Value.ClientSecret,
            RedirectUri = oidcOptions.Value.AuthorityCallbackUri.AbsolutePath
        };
        var response = await client.RequestAuthorizationCodeTokenAsync(request);
        if (response.IsError)
        {
            request.ClientSecret = "<removed>";
            logger.LogError(response.Exception, "Failed in acquiring token with request details: {@request}", request);
            throw new BadHttpRequestException(response.Error);
        }

        try
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
                ValidAudience = oidcOptions.Value.ClientId
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
            var descriptor = mapper.Map(user, response.AccessToken, response.IdentityToken);
            var token = await issuer.IssueAsync(descriptor);

            accessor.HttpContext!.Response.Cookies.Append("Authentication", token, new CookieOptions
            {
                IsEssential = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.Add(tokenOptions.Value.CookieDuration)
            });

            return Ok($"""<html><head><meta http-equiv="refresh" content="0; URL='{oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}'"/></head><body /></html>""");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", ErrorCode.Authentication.InvalidTokens)
            );
            return RedirectPreserveMethod(url);
        }
    }

    private static string ErrorCodeFrom(string? error, string? errorDescription) => (error?.ToLowerInvariant() ?? "", errorDescription?.ToLowerInvariant() ?? "")
    switch
    {
        ("access_denied", "no_ctx") => ErrorCode.AuthenticationUpstream.NoContext,
        ("access_denied", "user_aborted") or ("access_denied", "private_to_business_user_aborted") => ErrorCode.AuthenticationUpstream.Aborted,
        ("access_denied", "internal_error") or ("server_error", _) or ("temporarily_unavailable", _) => ErrorCode.AuthenticationUpstream.InternalError,
        ("unsupported_response_type", _) or ("invalid_request", _) => ErrorCode.AuthenticationUpstream.InvalidRequest,
        ("unauthorized_client", _) => ErrorCode.AuthenticationUpstream.InvalidClient,
        ("invalid_scope", _) => ErrorCode.AuthenticationUpstream.InvalidScope,
        _ => ErrorCode.AuthenticationUpstream.Failed,
    };
}
