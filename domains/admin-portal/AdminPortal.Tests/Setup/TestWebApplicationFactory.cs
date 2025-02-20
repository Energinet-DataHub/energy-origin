using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AdminPortal.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using RichardSzalay.MockHttp;

namespace AdminPortal.Tests.Setup;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MockHttpMessageHandler _mockHandler;

    public TestWebApplicationFactory()
    {
        _mockHandler = new MockHttpMessageHandler();

        _mockHandler.When("https://login.microsoftonline.com/*")
            .Respond("application/json", @"{""token_type"":""Bearer"",""expires_in"":3600,""access_token"":""fake-test-token""}");

        _mockHandler.When("*/first-party-organizations/")
            .Respond("application/json", @"{""result"":[{""organizationId"":""12345678-1234-1234-1234-123456789012"",""organizationName"":""Test Org"",""tin"":""12345678""}]}");

        _mockHandler.When("*/internal-contracts/")
            .Respond("application/json", @"{""result"":[{""gsrn"":""571234567891234567"",""meteringPointType"":""Production"",""meteringPointOwner"":""12345678-1234-1234-1234-123456789012"",""created"":1708430400,""startDate"":1708387200,""endDate"":null}]}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("INTERNAL_CLIENT_ID", "certificates-test-client-id")
            .UseSetting("INTERNAL_CLIENT_SECRET", "test-auth-client-secret")
            .UseSetting("INTERNAL_TENANT_ID", "f4abbcfa-9b38-4f05-b36d-6c3186193100");

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<MsalHttpClientFactoryAdapter>();
            services.AddSingleton<IMsalHttpClientFactory>(sp => sp.GetRequiredService<MsalHttpClientFactoryAdapter>());

            services.AddHttpClient("Msal").ConfigurePrimaryHttpMessageHandler(() => _mockHandler);
            services.AddHttpClient("FirstPartyApi").ConfigurePrimaryHttpMessageHandler(() => _mockHandler);
            services.AddHttpClient("ContractsApi").ConfigurePrimaryHttpMessageHandler(() => _mockHandler);

            services.AddTransient<IAuthenticationSchemeProvider, AutoFailSchemeProvider>();
            services.AddAuthentication(AutoFailSchemeProvider.AutoFailScheme)
                .AddScheme<AuthenticationSchemeOptions, AutoFailAuthHandler>(AutoFailSchemeProvider.AutoFailScheme, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient<T>(WebApplicationFactoryClientOptions options, int sessionId) where T : ImpersonatedUser
    {
        return CreateLoggedInClient<T>(options, list => list.Add(new Claim("sessionid", sessionId.ToString())));
    }

    private HttpClient CreateLoggedInClient<T>(WebApplicationFactoryClientOptions options, Action<List<Claim>> configure) where T : ImpersonatedUser
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddTransient<IAuthenticationSchemeProvider, InterceptOidcAuthenticationSchemeProvider>();
                services.AddAuthentication(InterceptOidcAuthenticationSchemeProvider.InterceptedScheme)
                    .AddScheme<ImpersonatedAuthenticationSchemeOptions, T>("InterceptedScheme", schemeOptions =>
                    {
                        schemeOptions.OriginalScheme = OpenIdConnectDefaults.AuthenticationScheme;
                        schemeOptions.Configure = configure;
                    });
            });
        }).CreateClient(options);
    }
}

public abstract class ImpersonatedUser(
    IOptionsMonitor<ImpersonatedAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ImpersonatedAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user"),
            new(ClaimTypes.Email, "testuser@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        Options.Configure?.Invoke(claims);

        var identity = new ClaimsIdentity(claims, OpenIdConnectDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, OpenIdConnectDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class ImpersonatedAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string OriginalScheme { get; set; } = null!;
    public Action<List<Claim>> Configure { get; set; } = null!;
}

public class GeneralUser(
    IOptionsMonitor<ImpersonatedAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : ImpersonatedUser(options, logger, encoder);
