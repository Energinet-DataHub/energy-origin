using System.Security.Claims;
using OpenIddict.Abstractions;

namespace Mock.Oidc.Models;

public class UserDescriptor
{
    public string? Subject { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public IEnumerable<Claim> ToClaims()
    {
        if (Subject != null) yield return new Claim(OpenIddictConstants.Claims.Subject, Subject);
        if (Name != null) yield return new Claim(OpenIddictConstants.Claims.Name, Name);
    }
}