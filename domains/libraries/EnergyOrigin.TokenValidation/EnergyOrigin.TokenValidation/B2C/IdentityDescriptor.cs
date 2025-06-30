using System.Security.Claims;
using EnergyOrigin.Setup.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace EnergyOrigin.TokenValidation.b2c;

public class IdentityDescriptor
{
    private readonly HttpContext _httpContext;
    private readonly ClaimsPrincipal _user;

    public IdentityDescriptor(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            throw new ForbiddenException("HttpContext is null");
        }

        _httpContext = httpContextAccessor.HttpContext;
        _user = _httpContext.User;
        ThrowExceptionIfUnsupportedAuthenticationScheme();
    }

    public Guid Subject => GetClaimAsGuid(ClaimType.Sub);

    public SubjectType SubjectType => GetClaimAsEnum<SubjectType>(ClaimType.SubType);

    public string Name => GetClaimAsString(ClaimType.Name);

    public string OrganizationName => GetClaimAsString(ClaimType.OrgName);
    public string OrganizationStatus => GetClaimAsString(ClaimType.OrgStatus);

    public string? OrganizationCvr => GetClaimAsOptionalString(ClaimType.OrgCvr);

    public IList<Guid> AuthorizedOrganizationIds => GetClaimAsGuidList(ClaimType.OrgIds);
    public Guid OrganizationId => GetClaimAsGuid(ClaimType.OrgId);

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
            return claimValue.Split(" ").Where(str => !string.IsNullOrEmpty(str) && Guid.TryParse(str, out _)).Select(Guid.Parse).ToList();
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

        return string.Empty;
    }

    private string? GetClaimAsOptionalString(string claimName)
    {
        return _user.FindFirstValue(claimName);
    }

    public bool IsTrial()
    {
        return OrganizationStatus == "trial";
    }

    public static bool IsSupported(HttpContext httpContext)
    {
        var usedAuthenticationScheme = GetUsedAuthenticationScheme(httpContext);
        var clientCredentialsScheme = AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme;
        var mitIdScheme = AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme;

        return usedAuthenticationScheme is not null &&
               (usedAuthenticationScheme.Contains(clientCredentialsScheme) || usedAuthenticationScheme.Contains(mitIdScheme));
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
