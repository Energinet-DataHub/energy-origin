using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Options;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public class TokenIssuer : ITokenIssuer
{
    public const string AllAcceptedScopes = $"{UserScopeClaim.AcceptedTerms} {UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}";

    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;

    public TokenIssuer(IOptions<TermsOptions> termsOptions, IOptions<TokenOptions> tokenOptions)
    {
        this.termsOptions = termsOptions.Value;
        this.tokenOptions = tokenOptions.Value;
    }

    public string Issue(UserDescriptor descriptor, bool versionBypass = false, DateTime? issueAt = default)
    {
        var credentials = CreateSigningCredentials(tokenOptions);

        var state = ResolveState(termsOptions, descriptor, versionBypass);

        return CreateToken(CreateTokenDescriptor(tokenOptions, credentials, descriptor, state, issueAt ?? DateTime.UtcNow));
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
            scope = string.Join(" ", scope, UserClaimName.AcceptedPrivacyPolicyVersion);
        }

        if (options.TermsOfServiceVersion != descriptor.AcceptedTermsOfServiceVersion)
        {
            scope = string.Join(" ", scope, UserClaimName.AcceptedTermsOfServiceVersion);
        }

        scope = versionBypass ? AllAcceptedScopes : scope ?? AllAcceptedScopes;

        return new(descriptor.AcceptedPrivacyPolicyVersion,descriptor.AcceptedTermsOfServiceVersion, scope);
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(TokenOptions tokenOptions, SigningCredentials credentials, UserDescriptor descriptor, UserState state, DateTime issueAt)
    {
        var claims = new Dictionary<string, object>
        {
            { UserClaimName.Scope, state.Scope },
            { UserClaimName.AccessToken, descriptor.EncryptedAccessToken },
            { UserClaimName.IdentityToken, descriptor.EncryptedIdentityToken },
            { UserClaimName.ProviderKeys, descriptor.EncryptedProviderKeys },
            { UserClaimName.ProviderType, descriptor.ProviderType.ToString() },
            { UserClaimName.AllowCprLookup, descriptor.AllowCprLookup },
            { UserClaimName.UserStored, descriptor.UserStored },
            { UserClaimName.Subject, descriptor.Subject },
            { UserClaimName.Actor, descriptor.Id },
            { UserClaimName.ActorLegacy, descriptor.Id },
        };

        if (state.AcceptedPrivacyPolicyTerms is not null )
        {
            claims.Add(UserClaimName.AcceptedPrivacyPolicyVersion, state.AcceptedPrivacyPolicyTerms);
        }

        if (state.AcceptedTermsOfServiceTerms is not null)
        {
            claims.Add(UserClaimName.AcceptedTermsOfServiceVersion, state.AcceptedTermsOfServiceTerms);
        }

        if (descriptor.Roles is not null)
        {
            claims.Add(UserClaimName.Roles, descriptor.Roles);
        }

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

    private record UserState(string? AcceptedPrivacyPolicyTerms, string? AcceptedTermsOfServiceTerms, string Scope);
}
