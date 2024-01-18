using API.Models.Entities;
using API.Options;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    public const string AllAcceptedScopes = $"{UserScopeName.Dashboard} {UserScopeName.Production} {UserScopeName.Meters} {UserScopeName.Certificates}";

    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;
    private readonly RoleOptions roleOptions;
    private readonly bool companyTermsFeatureFlag;
    private readonly TokenSigner tokenSigner;

    public TokenIssuer(TermsOptions termsOptions, TokenOptions tokenOptions, RoleOptions roleOptions, IFeatureManager? featureManager = null)
    {
        this.termsOptions = termsOptions;
        this.tokenOptions = tokenOptions;
        this.roleOptions = roleOptions;
        this.companyTermsFeatureFlag = featureManager.IsEnabled(FeatureFlag.CompanyTerms);
        this.tokenSigner = new TokenSigner(tokenOptions.PrivateKeyPem);
    }

    public string Issue(UserDescriptor descriptor, UserData data, bool versionBypass = false, DateTime? issueAt = default)
    {
        var state = ResolveState(termsOptions, data, versionBypass, companyTermsFeatureFlag);

        return CreateToken(tokenOptions, roleOptions, descriptor, data, state, issueAt ?? DateTime.UtcNow);
    }

    private static UserState ResolveState(TermsOptions options, UserData data, bool versionBypass, bool companyTermsFeatureFlag)
    {
        var scope = PrepareNotAcceptedScope(options, data, companyTermsFeatureFlag);
        scope = versionBypass ? AllAcceptedScopes : scope ?? AllAcceptedScopes;

        return new(scope);
    }

    private static string? PrepareNotAcceptedScope(TermsOptions options, UserData data, bool companyTermsFeatureFlag)
    {
        if (options.PrivacyPolicyVersion != data.PrivacyPolicyVersion)
        {
            return UserScopeName.NotAcceptedPrivacyPolicy;
        }

        if (companyTermsFeatureFlag)
        {
            if (options.TermsOfServiceVersion != data.TermsOfServiceVersion)
            {
                if (data.AssignedRoles != null && data.AssignedRoles.Contains(RoleKey.OrganizationAdmin))
                {
                    return UserScopeName.NotAcceptedTermsOfServiceOrganizationAdmin;
                }
                else
                {
                    return UserScopeName.NotAcceptedTermsOfService;
                }
            }
        }

        return null;
    }

    private string CreateToken(TokenOptions tokenOptions, RoleOptions roleOptions, UserDescriptor descriptor, UserData data, UserState state, DateTime issueAt)
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
            { UserClaimName.Subject, descriptor.Subject.ToString() },
            { UserClaimName.Actor, descriptor.Id.ToString() },
            { UserClaimName.ActorLegacy, descriptor.Id.ToString() }
        };

        var validRoles = roleOptions.RoleConfigurations.Select(x => x.Key);
        var assignedRoles = data.AssignedRoles ?? Array.Empty<string>();
        var matchedRoles = descriptor.MatchedRoles.Split(" ") ?? Array.Empty<string>();
        var allottedRoles = assignedRoles.Concat(matchedRoles).Distinct().Where(x => !x.IsNullOrEmpty());
        var roles = AddInheritedRoles(roleOptions.RoleConfigurations
            .ToDictionary(x => x.Key), allottedRoles)
            .Distinct()
            .Where(x => validRoles.Contains(x));

        claims.Add(UserClaimName.Roles, roles.ToList());

        if (descriptor.Organization is not null)
        {
            claims.Add(UserClaimName.OrganizationId, descriptor.Organization.Id.ToString());
            claims.Add(UserClaimName.Tin, descriptor.Organization.Tin);
            claims.Add(UserClaimName.OrganizationName, descriptor.Organization.Name);
        }

        return tokenSigner.Sign(
            descriptor.Subject.ToString(),
            descriptor.Name,
            tokenOptions.Issuer,
            tokenOptions.Audience,
            issueAt,
            (int)tokenOptions.Duration.TotalSeconds,
            claims
        );
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
