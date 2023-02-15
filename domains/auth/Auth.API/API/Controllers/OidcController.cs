using System.IdentityModel.Tokens.Jwt;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Utilities;
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
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
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
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters()
            {
                IssuerSigningKeys = discoveryDocument.KeySet.Keys.Select(it => it.ToSecurityKey()),
                ValidAudience = tokenOptions.Value.Audience,
                ValidIssuer = tokenOptions.Value.Issuer,
            };
            handler.ValidateToken(response.AccessToken, parameters, out _);
            handler.ValidateToken(response.IdentityToken, parameters, out _);
            var userInfo = handler.ValidateToken(response.TryGet("userinfo_token"), parameters, out _);
            // FIXME: validate tokens more?

            var providerId = userInfo.Claims.First(it => it.Type == "mitid.uuid").Value;
            var name = userInfo.Claims.First(it => it.Type == "mitid.identity_name").Value;
            var tin = userInfo.Claims.FirstOrDefault(it => it?.Type == "nemid.cvr", null)?.Value;

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
                Expires = DateTimeOffset.UtcNow.Add(oidcOptions.Value.CacheDuration) // FIXME: configurable
            });

            return Ok($"""<html><head><meta http-equiv="refresh" content="0; URL='{oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}'"/></head><body /></html>""");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2")
            );
            return RedirectPreserveMethod(url);
        }
    }
}
