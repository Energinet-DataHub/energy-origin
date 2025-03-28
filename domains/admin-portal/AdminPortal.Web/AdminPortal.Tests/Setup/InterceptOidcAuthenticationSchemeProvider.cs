using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
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

public static class AntiforgeryTestHelper
{
    public static async Task<string> ExtractAntiforgeryTokenAsync(this TestWebApplicationFactory factory, HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(html, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        if (!match.Success)
            throw new InvalidOperationException("Antiforgery token not found in response.");

        return match.Groups[1].Value;
    }
}
