using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace EnergyOrigin.TokenValidation.b2c;

public interface IIdentityDescriptor
{
    Guid Sub { get; }
    string Name { get; }
    string OrgName { get; }
    string? OrgCvr { get; }
    Guid OrgId { get; }
}

public class IdentityDescriptor : IIdentityDescriptor
{
    private readonly HttpContext _httpContext;
    private readonly Guid orgId;
    private readonly ClaimsPrincipal _user;

    public IdentityDescriptor(HttpContext httpContext, Guid orgId)
    {
        _httpContext = httpContext;
        this.orgId = orgId;
        _user = httpContext.User;
        ThrowExceptionIfUnsupportedAuthenticationScheme();

        if (!OrgIds.Contains(orgId))
        {
            throw new InvalidOperationException("IdentityDescriptor not supported");
        }
    }

    public Guid Sub => GetClaimAsGuid(ClaimType.Sub);

    public SubjectType SubType => GetClaimAsEnum<SubjectType>(ClaimType.SubType);

    public string Name => GetClaimAsString(ClaimType.Name);

    public string OrgName => GetClaimAsString(ClaimType.OrgName);

    public string? OrgCvr => GetClaimAsOptionalString(ClaimType.OrgCvr);

    public Guid OrgId => orgId;

    public IList<Guid> OrgIds => GetClaimAsGuidList(ClaimType.OrgIds);

    public IList<string> Scope => GetClaimAsStringList(ClaimType.OrgIds);

    private IList<string> GetClaimAsStringList(string claimName)
    {
        var claimValue = _user.FindFirstValue(claimName);
        if (claimValue is not null)
        {
            return claimValue.Split(" ").ToList();
        }

        throw new InvalidOperationException($"Unable to parse {claimValue} as {claimName}");
    }

    private IList<Guid> GetClaimAsGuidList(string claimName)
    {
        var claimValue = _user.FindFirstValue(claimName);
        if (claimValue is not null)
        {
            return claimValue.Split(" ").Select(Guid.Parse).ToList();
        }

        throw new InvalidOperationException($"Unable to parse {claimValue} as {claimName}");
    }

    private Guid GetClaimAsGuid(string claimName)
    {
        var claimValue = _user.FindFirstValue(claimName);
        if (claimValue is not null && Guid.TryParse(claimValue, out var sub))
        {
            return sub;
        }

        throw new InvalidOperationException($"Unable to parse {claimValue} as {claimName}");
    }

    private TEnum GetClaimAsEnum<TEnum>(string claimName) where TEnum : struct
    {
        var claimValue = _user.FindFirstValue(claimName);
        if (claimValue is not null && Enum.TryParse<TEnum>(claimValue, out var subType))
        {
            return subType;
        }

        throw new InvalidOperationException($"Unable to parse {claimValue} as {claimName}");
    }

    private string GetClaimAsString(string claimName)
    {
        var claimValue = _user.FindFirstValue(claimName);
        if (claimValue is not null)
        {
            return claimValue;
        }

        throw new InvalidOperationException($"Unable to parse {claimValue} as {claimValue}");
    }

    private string? GetClaimAsOptionalString(string claimName)
    {
        return _user.FindFirstValue(claimName);
    }

    public static bool IsSupported(HttpContext httpContext)
    {
        var usedAuthenticationScheme = GetUsedAuthenticationScheme(httpContext);
        var clientCredentialsScheme = AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme;
        var mitIdScheme = AuthenticationScheme.B2CMitICustomPolicyDAuthenticationScheme;

        return usedAuthenticationScheme == clientCredentialsScheme || usedAuthenticationScheme == mitIdScheme;
    }

    private static string? GetUsedAuthenticationScheme(HttpContext httpContext)
    {
        return httpContext.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Ticket?.AuthenticationScheme;
    }

    private void ThrowExceptionIfUnsupportedAuthenticationScheme()
    {
        if (!IsSupported(_httpContext))
        {
            throw new InvalidOperationException($"Authentication scheme {GetUsedAuthenticationScheme(_httpContext)} is not supported");
        }
    }
}
