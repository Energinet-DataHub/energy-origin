using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Options;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    public const string AllAcceptedScopes = $"{UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}";

    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;

    public TokenIssuer(TermsOptions termsOptions, TokenOptions tokenOptions)
    {
        this.termsOptions = termsOptions;
        this.tokenOptions = tokenOptions;
    }

    public string Issue(UserDescriptor descriptor, bool versionBypass = false, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = ResolveState(termsOptions, descriptor, versionBypass);

        return CreateToken(CreateTokenDescriptor(tokenOptions, credentials, termsOptions, descriptor, state, issueAt ?? DateTime.UtcNow));
    }

    private static SigningCredentials CreateSigningCredentials(TokenOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));

        var key = new RsaSecurityKey(rsa);

        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    private static UserState ResolveState(TermsOptions options, UserDescriptor descriptor, bool versionBypass)
    {
        string? scope = null;
        if (options.PrivacyPolicyVersion != descriptor.AcceptedPrivacyPolicyVersion)
        {
            scope = string.Join(" ", scope, UserScopeClaim.NotAcceptedPrivacyPolicy);
        }

        if (options.TermsOfServiceVersion != descriptor.AcceptedTermsOfServiceVersion)
        {
            scope = string.Join(" ", scope, UserScopeClaim.NotAcceptedTermsOfService);
        }

        scope = versionBypass ? AllAcceptedScopes : scope ?? AllAcceptedScopes;

        scope = scope.Trim();

        return new(descriptor.AcceptedPrivacyPolicyVersion, descriptor.AcceptedTermsOfServiceVersion, scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions tokenOptions, SigningCredentials credentials, TermsOptions termsOptions, UserDescriptor descriptor, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object>
        {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, descriptor.EncryptedAccessToken },
            { UserClaimName.IdentityToken, descriptor.EncryptedIdentityToken },
            { UserClaimName.AssignedRoles, descriptor.AssignedRoles },
            { UserClaimName.MatchedRoles, descriptor.MatchedRoles },
            { UserClaimName.CurrentPrivacyPolicyVersion, termsOptions.PrivacyPolicyVersion },
            { UserClaimName.CurrentTermsOfServiceVersion, termsOptions.TermsOfServiceVersion },
            { UserClaimName.AcceptedPrivacyPolicyVersion, state.AcceptedPrivacyPolicyVersion },
            { UserClaimName.AcceptedTermsOfServiceVersion, state.AcceptedTermsOfServiceVersion },
            { UserClaimName.ProviderKeys, descriptor.EncryptedProviderKeys },
            { UserClaimName.ProviderType, descriptor.ProviderType.ToString() },
            { UserClaimName.AllowCprLookup, descriptor.AllowCprLookup },
            { UserClaimName.Subject, descriptor.Subject },
            { UserClaimName.Actor, descriptor.Id },
            { UserClaimName.ActorLegacy, descriptor.Id },
        };

        var assignedRoles = descriptor.AssignedRoles.Split(" ") ?? Array.Empty<string>();
        var matchedRoles = descriptor.MatchedRoles.Split(" ") ?? Array.Empty<string>();

        claims.Add(UserClaimName.Roles, assignedRoles.Concat(matchedRoles).Distinct().Where(x => !x.IsNullOrEmpty()));

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

    private static string CreateToken(SecurityTokenDescriptor descriptor)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }

    private record UserState(int AcceptedPrivacyPolicyVersion, int AcceptedTermsOfServiceVersion, string Scope);
}
