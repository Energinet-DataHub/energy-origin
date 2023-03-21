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
        IUserDescriptorMapper mapper,
        IUserService service,
        ITokenIssuer issuer,
        IOptions<OidcOptions> oidcOptions,
        IOptions<TokenOptions> tokenOptions,
        IOptions<IdentityProviderOptions> providerOptions,
        ILogger<OidcController> logger,
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        if (code == null)
        {
            logger.LogWarning("Callback error: {error} - description: {errorDescription}", error, errorDescription);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.From(error, errorDescription)));
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
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
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.BadResponse));
        }

        string token;
        try
        {
            var descriptor = await MapUserDescriptor(mapper, service, providerOptions.Value, oidcOptions.Value, discoveryDocument, response);
            token = issuer.Issue(descriptor);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.Authentication.InvalidTokens)
            );
            return RedirectPreserveMethod(url);
        }

        accessor.HttpContext!.Response.Cookies.Append("Authentication", token, new CookieOptions
        {
            IsEssential = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.Add(tokenOptions.Value.CookieDuration)
        });

        return Ok($"""<html><head><meta http-equiv="refresh" content="0; URL='{oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}'"/></head><body /></html>""");
    }

    private static async Task<UserDescriptor> MapUserDescriptor(IUserDescriptorMapper mapper, IUserService service, IdentityProviderOptions providerOptions, OidcOptions oidcOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response)
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

        ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));

        if (subject != identity.FindFirstValue(JwtRegisteredClaimNames.Sub) || subject != userInfo.FindFirstValue(JwtRegisteredClaimNames.Sub))
        {
            throw new SecurityTokenException("Subject mismatched found in tokens received.");
        }

        var providerName = userInfo.FindFirstValue("idp");
        var identityType = userInfo.FindFirstValue("identity_type");

        ArgumentException.ThrowIfNullOrEmpty(providerName, nameof(providerName));
        ArgumentException.ThrowIfNullOrEmpty(identityType, nameof(identityType));

        if (providerOptions.Providers.Contains(IdentityProviderOptions.GetIdentityProviderEnum(providerName, identityType)) is false) throw new NotSupportedException();

        var scope = access.FindFirstValue("scope");
        var providerId = userInfo.FindFirstValue("idp_identity_id");

        ArgumentException.ThrowIfNullOrEmpty(scope, nameof(scope));
        ArgumentException.ThrowIfNullOrEmpty(providerId, nameof(providerId));

        var fullProviderId = $"{providerName}={providerId}";

        string? name = null;
        string? tin = null;
        string? companyName = null;

        switch (providerName)
        {
            case "nemid":
                name = userInfo.FindFirstValue("nemid.common_name");

                if (identityType == "professional")
                {
                    tin = userInfo.FindFirstValue("nemid.cvr");
                    companyName = userInfo.FindFirstValue("nemid.company_name");

                    ArgumentException.ThrowIfNullOrEmpty(tin, nameof(tin));
                    ArgumentException.ThrowIfNullOrEmpty(companyName, nameof(companyName));
                }
                break;
            case "mitid":
                name = userInfo.FindFirstValue("mitid.identity_name");

                if (identityType == "professional")
                {
                    throw new NotImplementedException();
                }
                break;
            default:
                throw new NotImplementedException();
        }

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        var user = await service.GetUserByProviderIdAsync(fullProviderId) ?? new User
        {
            Id = null,
            ProviderId = fullProviderId,
            Name = name,
            AcceptedTermsVersion = 0,
            AllowCPRLookup = false,
            Company = identityType == "professional"
                ? new Company()
                {
                    Id = null,
                    Tin = tin!,
                    Name = companyName!
                }
                : null
        };
        return mapper.Map(user, response.AccessToken, response.IdentityToken);
    }
}
