using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace AdminPortal.Tests.Setup;

public class InterceptOidcAuthenticationSchemeProvider : AuthenticationSchemeProvider
{
    public const string InterceptedScheme = "InterceptedScheme";

    public InterceptOidcAuthenticationSchemeProvider(IOptions<AuthenticationOptions> options)
        : base(options)
    {
    }

    protected InterceptOidcAuthenticationSchemeProvider(IOptions<AuthenticationOptions> options, IDictionary<string, AuthenticationScheme> schemes)
        : base(options, schemes)
    {
    }

    public override Task<AuthenticationScheme?> GetSchemeAsync(string name)
    {
        if (name == OpenIdConnectDefaults.AuthenticationScheme)
        {
            return base.GetSchemeAsync(InterceptedScheme);
        }

        return base.GetSchemeAsync(name);
    }
}
