using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using System.Web;
using Tests.Extensions;
using Xunit;

namespace Tests;

public class AuthorizationFlowIntegrationTest : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string ClientId = "energy-origin";
    private const string ClientSecret = "secret";
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
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Navigate to authorization endpoint

        var query = new QueryBuilder
        {
            { "client_id", ClientId },
            { "redirect_uri", RedirectUri },
            { "state", "testState" },
            { "response_type", "code" },
            { "scope", "openid nemid mitid ssn userinfo_token" },
            { "response_mode", "query" }
        };

        var requestUri = $"/Connect/Authorize{query}";
        var authorizeResponse = await client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var redirectLocation = authorizeResponse.Headers.Location!.OriginalString;
        Assert.StartsWith("/Connect/Signin", redirectLocation);

        // Follow the redirect to the signin page

        var signinPage = await client.GetAsync(redirectLocation);
        var signinDocument = await signinPage.GetHtmlDocument();

        Assert.Equal(HttpStatusCode.OK, signinPage.StatusCode);

        // Click on the first submit button, which matches the first user in test-users.yaml

        var formElement = (IHtmlFormElement)signinDocument.QuerySelector("form[id='user-selection-form']")!;
        var firstSubmitButtonElement = (IHtmlElement)signinDocument.QuerySelectorAll("button[type=submit]").First();
        var signupResponse = await client.SendAsync(formElement, firstSubmitButtonElement);

        Assert.Equal(HttpStatusCode.Redirect, signupResponse.StatusCode);
        Assert.StartsWith(RedirectUri, signupResponse.Headers.Location!.AbsoluteUri);

        var queryStringForCallback = HttpUtility.ParseQueryString(signupResponse.Headers.Location!.Query);
        var code = queryStringForCallback["code"]!;
        var callbackState = queryStringForCallback["state"];
        Assert.Equal("testState", callbackState);

        // Get token from endpoint using authorization code
        
        var tokenRequestData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", RedirectUri }
        });
        var tokenResponse = await client.PostAsync("/Connect/Token", tokenRequestData);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        // From the token response, extract id_token and userinfo_token

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(tokenJson).RootElement;

        var idTokenJwt = token.GetProperty("id_token").GetString()!;
        var idTokenJson = idTokenJwt.GetJwtPayload();
        var idToken = JsonDocument.Parse(idTokenJson).RootElement;
        
        Assert.Equal("7DADB7DB-0637-4446-8626-2781B06A9E20", idToken.GetProperty("sub").GetString());
        Assert.Equal(42, idToken.GetProperty("foo").GetInt32());

        var userInfoTokenJwt = token.GetProperty("userinfo_token").GetString()!;
        var userInfoTokenJson = userInfoTokenJwt.GetJwtPayload();
        var userInfoToken = JsonDocument.Parse(userInfoTokenJson).RootElement;

        Assert.Equal("7DADB7DB-0637-4446-8626-2781B06A9E20", userInfoToken.GetProperty("sub").GetString());
        Assert.Equal(42, userInfoToken.GetProperty("bar").GetInt32());

        // Logout

        var logoutResponse = await client.PostAsync("/Connect/Logout", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
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