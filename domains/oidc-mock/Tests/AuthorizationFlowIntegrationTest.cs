using System.Net;
using System.Text.Json;
using System.Web;
using AngleSharp.Html.Dom;
using Jose;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Oidc.Mock;
using Oidc.Mock.Extensions;
using Tests.TestHelpers;
using Xunit;

namespace Tests;

public class AuthorizationFlowIntegrationTest : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string ClientId = "energy-origin";
    private const string ClientSecret = "secret";
    private const string RedirectUri = "https://example.com/callback";

    public AuthorizationFlowIntegrationTest()
    {
        Environment.SetEnvironmentVariable(Configuration.UsersFilePathKey, "test-users.json");
        Environment.SetEnvironmentVariable(Configuration.ClientIdKey, ClientId);
        Environment.SetEnvironmentVariable(Configuration.ClientSecretKey, ClientSecret);
        Environment.SetEnvironmentVariable(Configuration.ClientRedirectUriKey, RedirectUri);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Test"));
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

        // Click on the first submit button, which matches the first user in test-users.json

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

        var authorization = $"{ClientId}:{ClientSecret}".EncodeBase64();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/Connect/Token");
        tokenRequest.Headers.Add("Authorization", $"Basic {authorization}");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", RedirectUri }
        });
        var tokenResponse = await client.SendAsync(tokenRequest);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        // From the token response, extract id_token and userinfo_token

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(tokenJson).RootElement;

        var idTokenJwt = token.GetProperty("id_token").GetString()!;
        var idTokenJson = idTokenJwt.GetJwtPayload();
        var idToken = JsonDocument.Parse(idTokenJson).RootElement;

        Assert.Equal("7DADB7DB-0637-4446-8626-2781B06A9E20", idToken.GetProperty("sub").GetString());
        Assert.Equal(42, idToken.GetProperty("some.number").GetInt32());

        var userInfoTokenJwt = token.GetProperty("userinfo_token").GetString()!;
        var userInfoTokenJson = userInfoTokenJwt.GetJwtPayload();
        var userInfoToken = JsonDocument.Parse(userInfoTokenJson).RootElement;

        Assert.Equal("7DADB7DB-0637-4446-8626-2781B06A9E20", userInfoToken.GetProperty("sub").GetString());
        Assert.Equal("42", userInfoToken.GetProperty("some.string").GetString());

        // Get JWK and verify signature

        var jwkResponse = await client.GetAsync(".well-known/openid-configuration/jwks");
        var jwkSet = JwkSet.FromJson(await jwkResponse.Content.ReadAsStringAsync(), new JsonMapper());
        var jwk = jwkSet.Keys.Single();

        Assert.NotNull(JWT.Decode(idTokenJwt, jwk));
        Assert.NotNull(JWT.Decode(userInfoTokenJwt, jwk));

        // Logout

        var logoutResponse = await client.PostAsync("/api/v1/session/logout", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Configuration.UsersFilePathKey, "");
        Environment.SetEnvironmentVariable(Configuration.ClientIdKey, "");
        Environment.SetEnvironmentVariable(Configuration.ClientSecretKey, "");
        Environment.SetEnvironmentVariable(Configuration.ClientRedirectUriKey, "");

        _factory.Dispose();
    }
}
