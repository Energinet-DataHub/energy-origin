using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace API.Controllers;

[ApiController]
public class OidcController : ControllerBase
{
    [HttpGet()]
    [Route("auth/oidc/callback")]
    public async Task<IActionResult> CallbackAsync(
        IDiscoveryCache discoveryCache,
        IHttpClientFactory clientFactory,
        IUserDescriptorMapper mapper,
        IUserProviderService userProviderService,
        IUserService userService,
        ITokenIssuer issuer,
        IOptions<OidcOptions> oidcOptions,
        IOptions<TokenOptions> tokenOptions,
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
            var descriptor = await MapUserDescriptor(mapper, userProviderService, userService, providerOptions.Value, oidcOptions.Value, discoveryDocument, response);
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

    private static async Task<UserDescriptor> MapUserDescriptor(IUserDescriptorMapper mapper, IUserProviderService userProviderService, IUserService userService, IdentityProviderOptions providerOptions, OidcOptions oidcOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response)
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

        var providerType = GetIdentityProviderEnum(providerName, identityType);

        if (providerOptions.Providers.Contains(providerType) is false)
        {
            throw new NotSupportedException("The selected ProviderType is not enabled.");
        }

        var scope = access.FindFirstValue("scope");

        ArgumentException.ThrowIfNullOrEmpty(scope, nameof(scope));

        string? name = null;
        string? tin = null;
        string? companyName = null;
        var keys = new Dictionary<ProviderKeyType, string>();

        switch (providerType)
        {
            case ProviderType.MitID_Professional:
                // TODO: Make sure this implementation is correct when implementing MitID Erhverv.

                //name = userInfo.FindFirstValue("mitid.identity_name");
                //tin = userInfo.FindFirstValue("nemlogin.cvr");
                //companyName = userInfo.FindFirstValue("nemlogin.org_name");

                //keys.Add(ProviderKeyType.RID, userInfo.FindFirstValue("nemlogin.nemid.rid") ?? throw new ArgumentNullException());
                //keys.Add(ProviderKeyType.EIA, userInfo.FindFirstValue("nemlogin.persistent_professional_id") ?? throw new ArgumentNullException());
                //keys.Add(ProviderKeyType.MitID_UUID, userInfo.FindFirstValue("mitid.uuid") ?? throw new ArgumentNullException());

                //break;

                throw new NotImplementedException("ProviderType.MitID_Professional hasn't been implemented yet.");
            case ProviderType.MitID_Private:
                name = userInfo.FindFirstValue("mitid.identity_name");

                if (userInfo.FindFirstValue("nemid.pid") is not null)
                {
                    keys.Add(ProviderKeyType.PID, userInfo.FindFirstValue("nemid.pid")!);
                }
                keys.Add(ProviderKeyType.MitID_UUID, userInfo.FindFirstValue("mitid.uuid") ?? throw new ArgumentNullException("mitid.uuid"));
                break;
            case ProviderType.NemID_Professional:
                name = userInfo.FindFirstValue("nemid.common_name");
                tin = userInfo.FindFirstValue("nemid.cvr");
                companyName = userInfo.FindFirstValue("nemid.company_name");

                keys.Add(ProviderKeyType.PID, userInfo.FindFirstValue("nemid.ssn") ?? throw new ArgumentNullException("nemid.ssn"));
                keys.Add(ProviderKeyType.RID, userInfo.FindFirstValue("nemid.rid") ?? throw new ArgumentNullException("nemid.rid"));
                break;
            case ProviderType.NemID_Private:
                name = userInfo.FindFirstValue("nemid.common_name");

                keys.Add(ProviderKeyType.MitID_UUID, userInfo.FindFirstValue("nemid.pid") ?? throw new ArgumentNullException("nemid.pid"));
                break;
            default:
                throw new NotSupportedException("An unsupported ProviderType was supplied.");
        }

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        if (identityType == ProviderGroup.Professional)
        {
            ArgumentException.ThrowIfNullOrEmpty(tin, nameof(tin));
            ArgumentException.ThrowIfNullOrEmpty(companyName, nameof(companyName));
        }

        var tokenUserProviders = UserProvider.GetUserProviders(keys);

        var user = (await userProviderService.FindUserProviderMatchAsync(tokenUserProviders))?.User ?? new User
        {
            Id = null,
            Name = name,
            AcceptedTermsVersion = 0,
            AllowCPRLookup = false,
            Company = identityType == ProviderGroup.Private ? null : new Company()
            {
                Id = null,
                Tin = tin!,
                Name = companyName!
            }
        };

        var newUserProviders = userProviderService.GetNonMatchingUserProviders(tokenUserProviders, user.UserProviders);

        user.UserProviders.AddRange(newUserProviders);

        if (user.Id is not null)
        {
            await userService.UpsertUserAsync(user);
        } 

        return mapper.Map(user, providerType, response.AccessToken, response.IdentityToken);
    }

    private static ProviderType GetIdentityProviderEnum(string providerName, string identityType) => (providerName, identityType) switch
    {
        (ProviderName.MitID, ProviderGroup.Private) => ProviderType.MitID_Private,
        (ProviderName.MitID, ProviderGroup.Professional) => ProviderType.MitID_Professional,
        (ProviderName.NemID, ProviderGroup.Private) => ProviderType.NemID_Private,
        (ProviderName.NemID, ProviderGroup.Professional) => ProviderType.NemID_Professional,
        _ => throw new NotImplementedException()
    };
}
