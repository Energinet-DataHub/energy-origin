using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Models.Entities;
using API.Options;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    public const string AllAcceptedScopes = $"{UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}";

    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;
    private readonly RoleOptions roleOptions;

    public TokenIssuer(TermsOptions termsOptions, TokenOptions tokenOptions, RoleOptions roleOptions)
    {
        this.termsOptions = termsOptions;
        this.tokenOptions = tokenOptions;
        this.roleOptions = roleOptions;
    }

    public string Issue(UserDescriptor descriptor, UserData data, bool versionBypass = false, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = ResolveState(termsOptions, data, versionBypass);

        return CreateToken(CreateTokenDescriptor(tokenOptions, roleOptions, credentials, descriptor, data, state, issueAt ?? DateTime.UtcNow));
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static UserState ResolveState(TermsOptions options, UserData data, bool versionBypass)
    {
        string? scope = null;
        if (options.PrivacyPolicyVersion != data.PrivacyPolicyVersion)
        {
            scope = string.Join(" ", scope, UserScopeClaim.NotAcceptedPrivacyPolicy);
        }

        scope = versionBypass ? AllAcceptedScopes : scope ?? AllAcceptedScopes;

        scope = scope.Trim();

        return new(scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions tokenOptions, RoleOptions roleOptions, SigningCredentials credentials, UserDescriptor descriptor, UserData data, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object>
        {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, descriptor.EncryptedAccessToken },
            { UserClaimName.IdentityToken, descriptor.EncryptedIdentityToken },
            { UserClaimName.MatchedRoles, descriptor.MatchedRoles },
            { UserClaimName.ProviderKeys, descriptor.EncryptedProviderKeys },
            { UserClaimName.ProviderType, descriptor.ProviderType.ToString() },
            { UserClaimName.AllowCprLookup, descriptor.AllowCprLookup },
            { UserClaimName.Subject, descriptor.Subject },
            { UserClaimName.Actor, descriptor.Id },
            { UserClaimName.ActorLegacy, descriptor.Id },
        };

        var validRoles = roleOptions.RoleConfigurations.Select(x => x.Key);
        var assignedRoles = data.AssignedRoles ?? Array.Empty<string>();
        var matchedRoles = descriptor.MatchedRoles.Split(" ") ?? Array.Empty<string>();
        var allottedRoles = assignedRoles.Concat(matchedRoles).Distinct().Where(x => !x.IsNullOrEmpty());
        var roles = AddInheritedRoles(roleOptions.RoleConfigurations.ToDictionary(x => x.Key), allottedRoles).Distinct().Where(x => validRoles.Contains(x));

        claims.Add(UserClaimName.Roles, roles);

        if (descriptor.CompanyId is not null)
        {
            claims.Add(UserClaimName.CompanyId, descriptor.CompanyId);
        }
        if (descriptor.Tin is not null)
        {
            claims.Add(UserClaimName.Tin, descriptor.Tin);
        }
        if (descriptor.CompanyName is not null)
        {
            claims.Add(UserClaimName.CompanyName, descriptor.CompanyName);
        }

        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, descriptor.Name),
            new(JwtRegisteredClaimNames.Sub, descriptor.Subject.ToString())
        };

        return new()
        {
            Subject = new ClaimsIdentity(identity),
            NotBefore = issueAt,
            Expires = issueAt.Add(tokenOptions.Duration),
            Issuer = tokenOptions.Issuer,
            Audience = tokenOptions.Audience,
            SigningCredentials = credentials,
            Claims = claims
        };
    }

    private static IEnumerable<string> AddInheritedRoles(Dictionary<string, RoleConfiguration> configurations, IEnumerable<string> roles)
    {
        if (roles.IsNullOrEmpty())
        {
            return Enumerable.Empty<string>();
        }
        var inherited = roles.Where(x => configurations[x].Inherits.IsNullOrEmpty() == false).SelectMany(x => configurations[x].Inherits);
        return roles.Concat(inherited).Concat(AddInheritedRoles(configurations, inherited));
    }

    private static string CreateToken(SecurityTokenDescriptor descriptor)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }

    public record UserData(int PrivacyPolicyVersion, int TermsOfServiceVersion, IEnumerable<string>? AssignedRoles = default)
    {
        public static UserData From(User? user) => new(
            user?.UserTerms.SingleOrDefault(x => x.Type == UserTermsType.PrivacyPolicy)?.AcceptedVersion ?? 0,
            user?.Company?.CompanyTerms.SingleOrDefault(x => x.Type == CompanyTermsType.TermsOfService)?.AcceptedVersion ?? 0,
            user?.UserRoles.Select(x => x.Role)
        );
    }

    private record UserState(string Scope);
}
