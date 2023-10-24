using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using static API.Utilities.TokenIssuer;

namespace API.Utilities;

internal static class OidcHelper
{
    public class RedirectionFlow : Exception
    {
        public required string Url { get; init; }

        [SetsRequiredMembers]
        public RedirectionFlow(string url) : base("") => Url = url;
    }

    internal record UserInfo(string Name, string? Tin, string? CompanyName, Dictionary<ProviderKeyType, string> Keys);

    internal static string BuildRedirectionUri(OidcOptions oidcOptions, OidcState? oidcState)
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

    internal static void TryVerifyCode(string? code, ILogger<OidcController> logger, string? error, string? errorDescription, string redirectionUri)
    {
        if (code == null)
        {
            logger.LogWarning("Callback error: {Error} - description: {ErrorDescription}", error, errorDescription);
            throw new RedirectionFlow(QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.From(error, errorDescription)));
        }
    }

    internal static void TryVerifyDiscoveryDocument(DiscoveryDocumentResponse? discoveryDocument, ILogger<OidcController> logger, string redirectionUri)
    {
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error ?? "Discovery document is null");
            throw new RedirectionFlow(QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }
    }

    internal static async Task<TokenResponse> FetchTokenResponse(IHttpClientFactory clientFactory, ILogger<OidcController> logger, OidcOptions oidcOptions, DiscoveryDocumentResponse discoveryDocument, string code, string redirectionUri)
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
            throw new RedirectionFlow(QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.BadResponse));
        }

        return response;
    }

    internal static async Task<(UserDescriptor, UserData)> BuildUserDescriptor(ILogger logger, ICryptography cryptography, IUserProviderService userProviderService, IUserService userService, IdentityProviderOptions providerOptions, OidcOptions oidcOptions, RoleOptions roleOptions, DiscoveryDocumentResponse discoveryDocument, TokenResponse response, string redirectionUri)
    {
        try
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

            var userInfoToken = response.TryGet("userinfo_token");
            var identityToken = response.IdentityToken;
            var accessToken = response.AccessToken;

            var userInfo = handler.ValidateToken(userInfoToken, parameters, out _);
            var identity = handler.ValidateToken(identityToken, parameters, out _);

            parameters.ValidateAudience = false;
            var access = handler.ValidateToken(accessToken, parameters, out _);

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

            var providerType = MatchIdentityProviderEnum(providerName, identityType);

            TryVerifyProviderType(providerType, providerOptions);

            var (name, tin, companyName, keys) = ExtractUserInfo(userInfo, providerType, identityType);

            var tokenUserProviders = UserProvider.ConvertDictionaryToUserProviders(keys);

            var user = await FetchOrCreateUserAndUpdateUserProvidersAsync(userService, userProviderService, tokenUserProviders, oidcOptions, subject, identityType, name, tin, companyName);

            var descriptor = user.MapDescriptor(cryptography, providerType, CalculateMatchedRoles(userInfo, roleOptions), response.AccessToken, response.IdentityToken);
            return (descriptor, UserData.From(user));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failure occured after acquiring token.");

            var url = new RequestUrl(discoveryDocument.EndSessionEndpoint).CreateEndSessionUrl(
                response.IdentityToken,
                QueryHelpers.AddQueryString(redirectionUri, ErrorCode.QueryString, ErrorCode.Authentication.InvalidTokens)
            );
            throw new RedirectionFlow(url);

        }
    }

    internal static void TryVerifyProviderType(ProviderType providerType, IdentityProviderOptions providerOptions)
    {
        if (providerOptions.Providers.Contains(providerType) == false)
        {
            throw new NotSupportedException($"Rejecting provider: {providerType}. Supported providers: {providerOptions.Providers}");
        }
    }

    internal static ProviderType MatchIdentityProviderEnum(string providerName, string identityType) => (providerName, identityType) switch
    {
        (ProviderName.MitId, ProviderGroup.Private) => ProviderType.MitIdPrivate,
        (ProviderName.MitIdProfessional, ProviderGroup.Professional) => ProviderType.MitIdProfessional,
        (ProviderName.NemId, ProviderGroup.Private) => ProviderType.NemIdPrivate,
        (ProviderName.NemId, ProviderGroup.Professional) => ProviderType.NemIdProfessional,
        _ => throw new NotImplementedException($"Could not resolve ProviderType based on ProviderName: '{providerName}' and IdentityType: '{identityType}'")
    };
    internal static UserInfo ExtractUserInfo(ClaimsPrincipal userInfo, ProviderType providerType, string? identityType)
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
        else if (tin != null || companyName != null)
        {
            throw new ArgumentException($"IdentityType: '{identityType}' is not allowed to have a tin or company name.");
        }

        return new UserInfo(name, tin, companyName, keys);
    }

    internal static async Task<User> FetchOrCreateUserAndUpdateUserProvidersAsync(
        IUserService userService,
        IUserProviderService userProviderService,
        List<UserProvider> tokenUserProviders,
        OidcOptions oidcOptions,
        string subject,
        string identityType,
        string name,
        string? tin,
        string? companyName)
    {
        var user = await userService.GetUserByIdAsync((await userProviderService.FindUserProviderMatchAsync(tokenUserProviders))?.UserId);
        var knownUser = user != null;
        Guid? subjectId = Guid.TryParse(subject, out var subjectGuid) ? subjectGuid : null;
        var userId = oidcOptions.ReuseSubject && identityType == ProviderGroup.Private ? subjectId : null;
        var companyId = oidcOptions.ReuseSubject && identityType != ProviderGroup.Private ? subjectId : Guid.NewGuid();
        user ??= new User
        {
            Id = userId,
            Name = name,
            AllowCprLookup = false,
            Company = identityType == ProviderGroup.Private
                ? null
                : new Company
                {
                    Id = companyId ?? Guid.NewGuid(),
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
}
