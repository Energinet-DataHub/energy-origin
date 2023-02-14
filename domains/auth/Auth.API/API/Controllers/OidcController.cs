using System.IdentityModel.Tokens.Jwt;
using API.Models;
using API.Options;
using API.Services;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<RedirectResult> CallbackAsync(
        IDiscoveryCache discoveryCache,
        HttpClient client, // FIXME: add
        IUserDescriptMapper mapper,
        IUserService service,
        ITokenIssuer issuer,
        IOptions<OidcOptions> options,
        ILogger<OidcController> logger,
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        if (code == null)
        {
            logger.LogWarning("Callback error: {error} - description: {errorDescription}", error, errorDescription);
            return RedirectPreserveMethod(options.Value.FrontendRedirectUri.AbsoluteUri); // FIXME: error code?
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(options.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
        }

        var request = new AuthorizationCodeTokenRequest
        {
            Address = discoveryDocument.TokenEndpoint,
            Code = code,
            ClientId = options.Value.ClientId,
            ClientSecret = options.Value.ClientSecret, // FIXME: add
            RedirectUri = options.Value.AuthorityCallbackUri.AbsolutePath
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
            var parameters = new TokenValidationParameters();
            new JwtSecurityTokenHandler().ValidateToken(response.AccessToken, parameters, out _);
            new JwtSecurityTokenHandler().ValidateToken(response.IdentityToken, parameters, out _);
            // FIXME: validate access and id tokens more?

            var userInfo = await client.GetUserInfoAsync(new UserInfoRequest // FIXME: may have impact on mock
            {
                Address = discoveryDocument.UserInfoEndpoint,
                Token = response.AccessToken
            });

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

            return RedirectPreserveMethod(options.Value.FrontendRedirectUri.AbsoluteUri); // FIXME: html redirect with cookie
        }
        catch
        {
            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                options.Value.FrontendRedirectUri.AbsoluteUri // FIXME: error code?
            );
            return RedirectPreserveMethod(url);
        }
    }
}
