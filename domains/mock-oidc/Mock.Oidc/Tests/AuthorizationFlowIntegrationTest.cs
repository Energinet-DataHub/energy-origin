using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Web;
using Tests.Extensions;
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
    public async Task CompleteFlowTest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })!;

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
        var redirectLocation = authorizeResponse.Headers.Location!.OriginalString;
        Assert.StartsWith("/Connect/Signin", redirectLocation);

        var signinPage = await client.GetAsync(redirectLocation);
        var signinDocument = await signinPage.GetHtmlDocument();
        Assert.Equal(HttpStatusCode.OK, signinPage.StatusCode);

        var submitButtonsSelector = signinDocument.QuerySelectorAll("button[type=submit]");
        Assert.Equal(2, submitButtonsSelector.Length);

        var signupResponse = await client.SendAsync(
            (IHtmlFormElement)signinDocument.QuerySelector("form[id='user-selection-form']")!, 
            (IHtmlElement)submitButtonsSelector.First());

        Assert.Equal(HttpStatusCode.Redirect, signupResponse.StatusCode);
        Assert.StartsWith(RedirectUri, signupResponse.Headers.Location!.AbsoluteUri);

        var queryStringForCallback = HttpUtility.ParseQueryString(signupResponse.Headers.Location!.Query);
        var code = queryStringForCallback["code"];

        Assert.NotNull(code);
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