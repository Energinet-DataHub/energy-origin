using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
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
using Microsoft.IdentityModel.Tokens;
using static API.Utilities.TokenIssuer;

namespace API.Controllers;

public class OidcException : Exception
{
    public string Url { get; init; }
    public OidcException(string message, string url) : base(message)
    {
        Url = url;
    }
}

[ApiController]
public class OidcController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [Route("auth/oidc/callback")]
    public async Task<IActionResult> CallbackAsync(
        IMetrics metrics,
        IDiscoveryCache discoveryCache,
        IHttpClientFactory clientFactory,
        IUserProviderService userProviderService,
        IUserService userService,
        ICryptography cryptography,
        ITokenIssuer issuer,
        OidcOptions oidcOptions,
        IdentityProviderOptions providerOptions,
        RoleOptions roleOptions,
        ILogger<OidcController> logger,
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        [FromQuery] string? state = default)
    {
        try
        {
            var oidcState = OidcState.Decode(state);

            var redirectionUri = RedirectionCheck(oidcOptions, oidcState);

            CodeNullCheck(code, logger, error, errorDescription, redirectionUri);

            var discoveryDocument = await discoveryCache.GetAsync();
            DiscoveryDocumentErrorChecks(discoveryDocument, logger, redirectionUri);

            var response = await GetClientAndResponse(clientFactory, logger, oidcOptions, discoveryDocument, code!, redirectionUri);

            var (descriptor, data) = await GetUserDescriptor(logger, cryptography, userProviderService, userService, providerOptions, oidcOptions, roleOptions, discoveryDocument, response, redirectionUri);

            var token = issuer.Issue(descriptor, data);

            logger.AuditLog(
                "{User} created token for {Subject} at {TimeStamp}.",
                descriptor.Id,
                descriptor.Subject,
                DateTimeOffset.Now.ToUnixTimeSeconds()
            );

            metrics.Login(descriptor.Id, descriptor.Organization?.Id, descriptor.ProviderType);

            return RedirectPreserveMethod(QueryHelpers.AddQueryString(redirectionUri, "token", token));
        }
        catch (OidcException e)
        {
            return RedirectPreserveMethod(e.Url);
        }
    }

    internal static async Task<(UserDescriptor, UserData)> MapUserDescriptor(ICryptography cryptography, IUserProviderService userProviderService, IUserService userService, IdentityProviderOptions providerOptions, OidcOptions oidcOptions, RoleOptions roleOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response)
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

        SubjectErrorCheck(subject, identity, userInfo);

        var providerName = userInfo.FindFirstValue("idp");
        var identityType = userInfo.FindFirstValue("identity_type");
        var scope = access.FindFirstValue(UserClaimName.Scope);

        ClaimsErrorCheck(scope, providerName, identityType);

        var providerType = GetIdentityProviderEnum(providerName!, identityType!);

        ProvidertypeIsFalseCheck(providerType, providerOptions);

        var (name, tin, companyName, keys) = HandleUserInfo(userInfo, providerType, identityType);

        var tokenUserProviders = UserProvider.ConvertDictionaryToUserProviders(keys);

        var user = await HandleUserAsync(userService, userProviderService, tokenUserProviders, oidcOptions, subject, identityType, name, tin, companyName);

        var descriptor = user.MapDescriptor(cryptography, providerType, CalculateMatchedRoles(userInfo, roleOptions), response.AccessToken, response.IdentityToken);
        return (descriptor, UserData.From(user));
    }

    internal static IEnumerable<string> CalculateMatchedRoles(ClaimsPrincipal info, RoleOptions options) => options.RoleConfigurations.Select(role => role.Matches.Any(match =>
    {
        var property = info.FindFirstValue(match.Property);
        return match.Operator switch
        {
            "exists" => property != null,
            "contains" => property?.ToLowerInvariant().Contains(match.Value.ToLowerInvariant()) ?? false,
            "equals" => property?.ToLowerInvariant().Equals(match.Value.ToLowerInvariant()) ?? false,
            _ => false
        };
    }) ? role.Key : null).OfType<string>();

    internal static ProviderType GetIdentityProviderEnum(string providerName, string identityType) => (providerName, identityType) switch
    {
        (ProviderName.MitId, ProviderGroup.Private) => ProviderType.MitIdPrivate,
        (ProviderName.MitIdProfessional, ProviderGroup.Professional) => ProviderType.MitIdProfessional,
        (ProviderName.NemId, ProviderGroup.Private) => ProviderType.NemIdPrivate,
        (ProviderName.NemId, ProviderGroup.Professional) => ProviderType.NemIdProfessional,
        _ => throw new NotImplementedException($"Could not resolve ProviderType based on ProviderName: '{providerName}' and IdentityType: '{identityType}'")
    };

    internal static string RedirectionCheck(OidcOptions oidcOptions, OidcState? oidcState)
    {
        var redirectionUri = oidcOptions.FrontendRedirectUri.AbsoluteUri;
        if (oidcState?.RedirectionPath != null)
        {
            redirectionUri = QueryHelpers.AddQueryString(redirectionUri, "redirectionPath", oidcState.RedirectionPath.Trim('/'));
        }
        if (oidcOptions.AllowRedirection && oidcState?.RedirectionUri != null)
        {
            redirectionUri = oidcState.RedirectionUri;
        }
        if (oidcState?.State != null)
        {
            redirectionUri = QueryHelpers.AddQueryString(redirectionUri, "state", oidcState.State);
        }
        return redirectionUri;
    }

    //DONE
    internal static void CodeNullCheck(string? code, ILogger<OidcController> logger, string? error, string? errorDescription, string redirectionUri)
    {
        if (code == null)
        {
            logger.LogWarning("Callback error: {Error} - description: {ErrorDescription}", error, errorDescription);
            throw new OidcException("Code is null", QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.From(error, errorDescription)));
        }
    }

    //DONE
    internal static void DiscoveryDocumentErrorChecks(DiscoveryDocumentResponse? discoveryDocument, ILogger<OidcController> logger, string redirectionUri)
    {
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            throw new OidcException("Discovery document is null", QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }
    }

    //DONE
    internal static async Task<TokenResponse> GetClientAndResponse(IHttpClientFactory clientFactory, ILogger<OidcController> logger, OidcOptions oidcOptions, DiscoveryDocumentResponse discoveryDocument, string code, string redirectionUri)
    {
        var client = clientFactory.CreateClient();

        var request = new AuthorizationCodeTokenRequest
        {
            Address = discoveryDocument.TokenEndpoint,
            Code = code,
            ClientId = oidcOptions.ClientId,
            ClientSecret = oidcOptions.ClientSecret,
            RedirectUri = oidcOptions.AuthorityCallbackUri.AbsoluteUri
        };

        var response = await client.RequestAuthorizationCodeTokenAsync(request);

        if (response.IsError)
        {
            logger.LogError(response.Exception, "Failed in acquiring token with request details: {@Request}", request);
            throw new OidcException("Response is error", QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.BadResponse));
        }

        return response;
    }

    internal static async Task<(UserDescriptor, UserData)> GetUserDescriptor(ILogger<OidcController> logger, ICryptography cryptography, IUserProviderService userProviderService, IUserService userService, IdentityProviderOptions providerOptions, OidcOptions oidcOptions, RoleOptions roleOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response, string redirectionUri)
    {
        try
        {
            return await MapUserDescriptor(cryptography, userProviderService, userService, providerOptions, oidcOptions, roleOptions, discoveryDocument, response);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token.");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.Authentication.InvalidTokens)
            );
            throw new OidcException("token error", url);
        }
    }

    internal static void SubjectErrorCheck(string? subject, ClaimsPrincipal identity, ClaimsPrincipal userInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));
        if (subject != identity.FindFirstValue(JwtRegisteredClaimNames.Sub) || subject != userInfo.FindFirstValue(JwtRegisteredClaimNames.Sub))
        {
            throw new SecurityTokenException("Subject mismatched found in tokens received.");
        }
    }

    internal static void ProvidertypeIsFalseCheck(ProviderType providerType, IdentityProviderOptions providerOptions)
    {
        if (providerOptions.Providers.Contains(providerType) == false)
        {
            throw new NotSupportedException($"Rejecting provider: {providerType}. Supported providers: {providerOptions.Providers}");
        }
    }

    internal static void ClaimsErrorCheck(string? scope, string? providerName, string? identityType)
    {
        ArgumentException.ThrowIfNullOrEmpty(scope, nameof(scope));
        ArgumentException.ThrowIfNullOrEmpty(providerName, nameof(providerName));
        ArgumentException.ThrowIfNullOrEmpty(identityType, nameof(identityType));
    }

    internal static (string? name, string? tin, string? companyName, Dictionary<ProviderKeyType, string>) HandleUserInfo(ClaimsPrincipal userInfo, ProviderType providerType, string? identityType)
    {
        string? name = null;
        string? tin = null;
        string? companyName = null;
        var keys = new Dictionary<ProviderKeyType, string>();
        switch (providerType)
        {
            case ProviderType.MitIdProfessional:
                name = userInfo.FindFirstValue("nemlogin.name");
                tin = userInfo.FindFirstValue("nemlogin.cvr");
                companyName = userInfo.FindFirstValue("nemlogin.org_name");
                var rid = userInfo.FindFirstValue("nemlogin.nemid.rid");
                if (tin is not null && rid is not null)
                {
                    keys.Add(ProviderKeyType.Rid, $"CVR:{tin}-RID:{rid}");
                }

                keys.Add(ProviderKeyType.Eia, userInfo.FindFirstValue("nemlogin.persistent_professional_id") ?? throw new KeyNotFoundException("nemlogin.persistent_professional_id"));

                break;
            case ProviderType.MitIdPrivate:
                name = userInfo.FindFirstValue("mitid.identity_name");

                var pid = userInfo.FindFirstValue("nemid.pid");
                if (pid is not null)
                {
                    keys.Add(ProviderKeyType.Pid, pid);
                }

                keys.Add(ProviderKeyType.MitIdUuid, userInfo.FindFirstValue("mitid.uuid") ?? throw new KeyNotFoundException("mitid.uuid"));
                break;
            case ProviderType.NemIdProfessional:
                name = userInfo.FindFirstValue("nemid.common_name");
                tin = userInfo.FindFirstValue("nemid.cvr");
                companyName = userInfo.FindFirstValue("nemid.company_name");

                keys.Add(ProviderKeyType.Rid, userInfo.FindFirstValue("nemid.ssn") ?? throw new KeyNotFoundException("nemid.ssn"));
                break;
            case ProviderType.NemIdPrivate:
                name = userInfo.FindFirstValue("nemid.common_name");

                keys.Add(ProviderKeyType.Pid, userInfo.FindFirstValue("nemid.pid") ?? throw new KeyNotFoundException("nemid.pid"));
                break;
        }

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        if (identityType == ProviderGroup.Professional)
        {
            ArgumentException.ThrowIfNullOrEmpty(tin, nameof(tin));
            ArgumentException.ThrowIfNullOrEmpty(companyName, nameof(companyName));
        }

        return (name, tin, companyName, keys);
    }

    internal static async Task<User> HandleUserAsync(
        IUserService userService,
        IUserProviderService userProviderService,
        List<UserProvider> tokenUserProviders,
        OidcOptions oidcOptions,
        string? subject,
        string? identityType,
        string? name,
        string? tin,
        string? companyName)
    {
        var user = await userService.GetUserByIdAsync((await userProviderService.FindUserProviderMatchAsync(tokenUserProviders))?.UserId);
        var knownUser = user != null;
        user ??= new User
        {
            Id = oidcOptions.ReuseSubject && Guid.TryParse(subject, out var subjectId) ? subjectId : null,
            Name = name!,
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

        return user;
    }
}
