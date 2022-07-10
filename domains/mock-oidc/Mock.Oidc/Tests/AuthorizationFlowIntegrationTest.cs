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
        var callbackState = queryStringForCallback["state"];

        Assert.Equal("testState", callbackState);

        //TODO: Figure out how Token-endpoint can accept json
        //var json = JsonSerializer.Serialize(new
        //{
        //    code = code, 
        //    client_id = ClientId, 
        //    client_secret = ClientSecret, 
        //    redirect_uri = RedirectUri
        //});
        //var data = new StringContent(json, Encoding.UTF8, "application/json");
        var data = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "redirect_uri", RedirectUri }
        });
        var tokenResponse = await client.PostAsync("/Connect/Token", data);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(tokenJson).RootElement;

        var idJwtToken = jsonDocument.GetProperty("id_token").GetString();
        var idTokenJson = idJwtToken.GetJwtPayload();
        var idToken = JsonDocument.Parse(idTokenJson).RootElement;
        
        Assert.Equal("7DADB7DB-0637-4446-8626-2781B06A9E20", idToken.GetProperty("sub").GetString());
        Assert.Equal(42, idToken.GetProperty("foo").GetInt32());

        var userInfoJwtToken = jsonDocument.GetProperty("userinfo_token").GetString();
        var userInfoTokenJson = userInfoJwtToken.GetJwtPayload();
        var userInfoToken = JsonDocument.Parse(userInfoTokenJson).RootElement;

        Assert.Equal("7DADB7DB-0637-4446-8626-2781B06A9E20", userInfoToken.GetProperty("sub").GetString());
        Assert.Equal(42, userInfoToken.GetProperty("bar").GetInt32());

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