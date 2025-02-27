using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AdminPortal.Dtos;
using AdminPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminPortal.Tests.Setup;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.First(s => s.ServiceType == typeof(IAuthorizationService)));
            services.Remove(services.First(s => s.ServiceType == typeof(ICertificatesService)));
            services.AddScoped<IAuthorizationService, MockAuthorizationService>();
            services.AddScoped<ICertificatesService, MockCertificatesService>();

            services.AddTransient<IAuthenticationSchemeProvider, AutoFailSchemeProvider>();
            services.AddAuthentication(AutoFailSchemeProvider.AutoFailScheme)
                .AddScheme<AuthenticationSchemeOptions, AutoFailAuthHandler>(AutoFailSchemeProvider.AutoFailScheme, _ => { });
        });
    }

    private class MockAuthorizationService : IAuthorizationService
    {
        public Task<FirstPartyOrganizationsResponse> GetOrganizationsAsync()
        {
            return Task.FromResult(
                new FirstPartyOrganizationsResponse(new List<FirstPartyOrganizationsResponseItem>()));
        }
    }

    private class MockCertificatesService : ICertificatesService
    {
        public Task<ContractsForAdminPortalResponse> GetContractsAsync()
        {
            return Task.FromResult(
                new ContractsForAdminPortalResponse(new List<ContractsForAdminPortalResponseItem>()));
        }
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
                    .AddScheme<ImpersonatedAuthenticationSchemeOptions, T>(InterceptOidcAuthenticationSchemeProvider.InterceptedScheme, schemeOptions =>
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

        Options.Configure.Invoke(claims);

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
