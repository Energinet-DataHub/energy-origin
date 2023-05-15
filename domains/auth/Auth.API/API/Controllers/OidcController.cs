using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
public class OidcController : ControllerBase
{
    [HttpGet]
    [Route("auth/oidc/callback")]
    public async Task<IActionResult> CallbackAsync(
        IDiscoveryCache discoveryCache,
        IHttpClientFactory clientFactory,
        IUserDescriptorMapper mapper,
        IUserProviderService userProviderService,
        IUserService userService,
        ITokenIssuer issuer,
        IOptions<OidcOptions> oidcOptions,
        IOptions<IdentityProviderOptions> providerOptions,
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
            redirectionUri = oidcState.RedirectionUri;
        }

        if (code == null)
        {
            // hehe
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
            var descriptor = await MapUserDescriptor(mapper, userProviderService, userService, providerOptions.Value, oidcOptions.Value, discoveryDocument, response);
            token = issuer.Issue(descriptor);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token.");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.Authentication.InvalidTokens)
            );
            return RedirectPreserveMethod(url);
        }

        if (oidcState?.State != null)
        {
            redirectionUri = QueryHelpers.AddQueryString(redirectionUri, "state", oidcState.State);
        }

        return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, "token", token));
    }

    private static async Task<UserDescriptor> MapUserDescriptor(IUserDescriptorMapper mapper, IUserProviderService userProviderService, IUserService userService, IdentityProviderOptions providerOptions, OidcOptions oidcOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response)
    {
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };
        var parameters = new TokenValidationParameters
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
        var scope = access.FindFirstValue(UserClaimName.Scope);

        ArgumentException.ThrowIfNullOrEmpty(scope, nameof(scope));
        ArgumentException.ThrowIfNullOrEmpty(providerName, nameof(providerName));
        ArgumentException.ThrowIfNullOrEmpty(identityType, nameof(identityType));

        var providerType = GetIdentityProviderEnum(providerName, identityType);
        if (!providerOptions.Providers.Contains(providerType))
        {
            throw new NotSupportedException($"Rejecting provider: {providerType}. Supported providers: {providerOptions.Providers}");
        }

        string? name = null;
        string? tin = null;
        string? companyName = null;
        var keys = new Dictionary<ProviderKeyType, string>();

        switch (providerType)
        {
            case ProviderType.MitID_Professional:
                name = userInfo.FindFirstValue("nemlogin.name");
                tin = userInfo.FindFirstValue("nemlogin.cvr");
                companyName = userInfo.FindFirstValue("nemlogin.org_name");

                var rid = userInfo.FindFirstValue("nemlogin.nemid.rid");
                if (tin is not null && rid is not null)
                {
                    keys.Add(ProviderKeyType.RID, $"CVR:{tin}-RID:{rid}");
                }
                keys.Add(ProviderKeyType.EIA, userInfo.FindFirstValue("nemlogin.persistent_professional_id") ?? throw new KeyNotFoundException("nemlogin.persistent_professional_id"));

                break;
            case ProviderType.MitID_Private:
                name = userInfo.FindFirstValue("mitid.identity_name");

                var pid = userInfo.FindFirstValue("nemid.pid");
                if (pid is not null)
                {
                    keys.Add(ProviderKeyType.PID, pid);
                }
                keys.Add(ProviderKeyType.MitID_UUID, userInfo.FindFirstValue("mitid.uuid") ?? throw new KeyNotFoundException("mitid.uuid"));
                break;
            case ProviderType.NemID_Professional:
                name = userInfo.FindFirstValue("nemid.common_name");
                tin = userInfo.FindFirstValue("nemid.cvr");
                companyName = userInfo.FindFirstValue("nemid.company_name");

                keys.Add(ProviderKeyType.RID, userInfo.FindFirstValue("nemid.ssn") ?? throw new KeyNotFoundException("nemid.ssn"));
                break;
            case ProviderType.NemID_Private:
                name = userInfo.FindFirstValue("nemid.common_name");

                keys.Add(ProviderKeyType.PID, userInfo.FindFirstValue("nemid.pid") ?? throw new KeyNotFoundException("nemid.pid"));
                break;
        }

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        if (identityType == ProviderGroup.Professional)
        {
            ArgumentException.ThrowIfNullOrEmpty(tin, nameof(tin));
            ArgumentException.ThrowIfNullOrEmpty(companyName, nameof(companyName));
        }

        var tokenUserProviders = UserProvider.ConvertDictionaryToUserProviders(keys);

        var user = await userService.GetUserByIdAsync((await userProviderService.FindUserProviderMatchAsync(tokenUserProviders))?.UserId);
        var knownUser = user != null;
        user ??= new User
        {
            Id = oidcOptions.ReuseSubject && Guid.TryParse(subject, out var subjectId) ? subjectId : null,
            Name = name,
            AcceptedTermsVersion = 0,
            AllowCprLookup = false,
            Company = identityType == ProviderGroup.Private
                ? null
                : new Company
                {
                    Id = null,
                    Tin = tin!,
                    Name = companyName!
                }
        };

        var newUserProviders = userProviderService.GetNonMatchingUserProviders(tokenUserProviders, user.UserProviders);

        user.UserProviders.AddRange(newUserProviders);

        if (knownUser)
        {
            await userService.UpsertUserAsync(user);
        }

        return mapper.Map(user, providerType, response.AccessToken, response.IdentityToken);
    }

    private static ProviderType GetIdentityProviderEnum(string providerName, string identityType) => (providerName, identityType) switch
    {
        (ProviderName.MitId, ProviderGroup.Private) => ProviderType.MitID_Private,
        (ProviderName.MitIdProfessional, ProviderGroup.Professional) => ProviderType.MitID_Professional,
        (ProviderName.NemId, ProviderGroup.Private) => ProviderType.NemID_Private,
        (ProviderName.NemId, ProviderGroup.Professional) => ProviderType.NemID_Professional,
        _ => throw new NotImplementedException($"Could not resolve ProviderType based on ProviderName: '{providerName}' and IdentityType: '{identityType}'")
    };
}
