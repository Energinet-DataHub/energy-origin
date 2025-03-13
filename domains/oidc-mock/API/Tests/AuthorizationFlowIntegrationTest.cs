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
    private readonly WebApplicationFactory<Program> factory;

    private const string clientId = "energy-origin";
    private const string clientSecret = "secret";
    private const string redirectUri = "https://example.com/callback";

    public AuthorizationFlowIntegrationTest() => factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseEnvironment("Test")
            .UseSetting(Configuration.UsersFilePathKey, "test-users.json")
            .UseSetting($"{Configuration.ClientsPrefix}:0:{Configuration.ClientIdKey}", clientId)
            .UseSetting($"{Configuration.ClientsPrefix}:0:{Configuration.ClientSecretKey}", clientSecret)
            .UseSetting($"{Configuration.ClientsPrefix}:0:{Configuration.ClientRedirectUriKey}", redirectUri));

    [Fact]
    public async Task CompleteFlowTest()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Navigate to authorization endpoint

        var query = new QueryBuilder
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "state", "testState" },
            { "response_type", "code" },
            { "scope", "openid nemid mitid ssn userinfo_token" },
            { "response_mode", "query" }
        };

        var requestUri = $"/Connect/Authorize{query}";
        var authorizeResponse = await client.GetAsync(requestUri, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var redirectLocation = authorizeResponse.Headers.Location!.OriginalString;
        Assert.StartsWith("/Connect/Signin", redirectLocation);

        // Follow the redirect to the signin page

        var signinPage = await client.GetAsync(redirectLocation, TestContext.Current.CancellationToken);
        var signinDocument = await signinPage.GetHtmlDocument();

        Assert.Equal(HttpStatusCode.OK, signinPage.StatusCode);

        // Click on the first submit button, which matches the first user in test-users.json

        var formElement = (IHtmlFormElement)signinDocument.QuerySelector("form[id='user-selection-form']")!;
        var firstSubmitButtonElement = (IHtmlElement)signinDocument.QuerySelectorAll("button[type=submit]").First();
        var signupResponse = await client.SendAsync(formElement, firstSubmitButtonElement);

        Assert.Equal(HttpStatusCode.Redirect, signupResponse.StatusCode);
        Assert.StartsWith(redirectUri, signupResponse.Headers.Location!.AbsoluteUri);

        var queryStringForCallback = HttpUtility.ParseQueryString(signupResponse.Headers.Location!.Query);
        var code = queryStringForCallback["code"]!;
        var callbackState = queryStringForCallback["state"];
        Assert.Equal("testState", callbackState);

        // Get token from endpoint using authorization code

        var authorization = $"{clientId}:{clientSecret}".EncodeBase64();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/Connect/Token");
        tokenRequest.Headers.Add("Authorization", $"Basic {authorization}");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri }
        });
        var tokenResponse = await client.SendAsync(tokenRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        // From the token response, extract id_token and userinfo_token

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
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

        var jwkResponse = await client.GetAsync(".well-known/openid-configuration/jwks", TestContext.Current.CancellationToken);
        var jwkSet = JwkSet.FromJson(await jwkResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), new JsonMapper());
        var jwk = jwkSet.Keys.Single();

        Assert.NotNull(JWT.Decode(idTokenJwt, jwk));
        Assert.NotNull(JWT.Decode(userInfoTokenJwt, jwk));

        // Logout

        var logoutResponse = await client.PostAsync("/api/v1/session/logout", new StringContent(""), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
