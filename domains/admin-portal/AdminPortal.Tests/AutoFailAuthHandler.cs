namespace AdminPortal.Tests;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

public class AutoFailAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.Fail("OIDC authentication is disabled in tests."));
    }
}

public class AutoFailSchemeProvider : AuthenticationSchemeProvider
{
    public const string AutoFailScheme = "AutoFail";

    public AutoFailSchemeProvider(IOptions<AuthenticationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthenticationScheme?> GetSchemeAsync(string name)
    {
        return Task.FromResult(new AuthenticationScheme(AutoFailScheme, AutoFailScheme, typeof(AutoFailAuthHandler)))!;
    }
}
