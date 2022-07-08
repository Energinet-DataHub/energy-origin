using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Tests;

public class AuthorizationFlowIntegrationTest : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string ClientId = "energy-origin";
    private const string ClientSecret = "secret_secret_secret";
    private const string RedirectUri = "https://www.foo.com/callback";

    public AuthorizationFlowIntegrationTest()
    {
        Environment.SetEnvironmentVariable("USERS_FILE_PATH", "test-users.yaml");
        Environment.SetEnvironmentVariable("CLIENT_ID", ClientId);
        Environment.SetEnvironmentVariable("CLIENT_SECRET", ClientSecret);
        Environment.SetEnvironmentVariable("CLIENT_REDIRECT_URI", RedirectUri);

        _factory = new WebApplicationFactory<Program>();
    }

    [Fact]
    public async Task AuthorizeRedirectsToSignin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var query = new QueryBuilder
        {
            { "client_id", ClientId },
            { "redirect_uri", RedirectUri },
            { "state", "foo" },
            { "response_type", "code" },
            { "scope", "openid nemid mitid ssn userinfo_token" },
            { "response_mode", "query" }
        };

        var requestUri = $"/Connect/Authorize{query}";
        var authorizeResponse = await client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        Assert.StartsWith("/Connect/Signin", authorizeResponse.Headers.Location!.OriginalString);
    }
    
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("USERS_FILE_PATH", "");
        Environment.SetEnvironmentVariable("CLIENT_ID", "");
        Environment.SetEnvironmentVariable("CLIENT_SECRET", "");
        Environment.SetEnvironmentVariable("CLIENT_REDIRECT_URI", "");

        _factory.Dispose();
    }
}