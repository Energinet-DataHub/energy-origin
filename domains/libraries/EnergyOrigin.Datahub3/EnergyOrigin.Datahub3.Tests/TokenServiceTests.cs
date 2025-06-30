using Xunit;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using Microsoft.Extensions.Time.Testing;
using System.Text.Json;

namespace EnergyOrigin.Datahub3.Tests;

public class TokenServiceTests
{
    private readonly DataHub3Options _dataHub3Options;
    private readonly FakeTimeProvider _fakeTimeProvider;

    private readonly JsonSerializerOptions _jsonSerialilzerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _baseUrl = "https://b2c.com";

    public TokenServiceTests()
    {
        _dataHub3Options = new DataHub3Options()
        {
            TokenUrl = "https://b2c.com/token",
            Scope = "testscope",
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = "testclientsecret",
            EnableMock = false
        };

        _fakeTimeProvider = new FakeTimeProvider();
    }

    [Fact]
    public async Task GetToken_WhenMockEnabled_ReturnsGuid()
    {
        // Arrange
        var token = new Token("FakeAccessToken", 100L);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(HttpStatusCode.OK, new StringContent(JsonSerializer.Serialize(token, _jsonSerialilzerOptions)));
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        _dataHub3Options.EnableMock = true;
        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider);

        // Act
        var accessToken = await tokenService.GetToken();

        // Assert
        Assert.True(Guid.TryParse(accessToken, out _));
    }

    [Fact]
    public async Task GetToken_WhenNoTokenFetchedYet_ReturnsNewToken()
    {
        // Arrange
        var token = new Token("FakeAccessToken", 100L);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(HttpStatusCode.OK, new StringContent(JsonSerializer.Serialize(token, _jsonSerialilzerOptions)));
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider);

        // Act
        var accessToken = await tokenService.GetToken();

        // Assert
        Assert.Equal(token.AccessToken, accessToken);
    }

    [InlineData(1800)]
    [InlineData(1801)]
    [InlineData(3600)]
    [Theory]
    public async Task GetToken_WhenTokenExistsAndIsStillValid_ReturnsCurrentToken(int seconds)
    {
        // Arrange
        var currentTime = new DateTime(2025, 12, 5, 7, 20, 0).ToUniversalTime();
        _fakeTimeProvider.SetUtcNow(currentTime);

        long expiresInUnixTime = ((DateTimeOffset)currentTime).AddSeconds(seconds).ToUnixTimeSeconds();
        var token = new Token("FakeAccessToken", expiresInUnixTime);
        var token2 = new Token("FakeAccessToken2", expiresInUnixTime);
        var tokens = new[] { token, token2 };

        var callCount = 0;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(req =>
            {
                var token = tokens[callCount++];
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(token, _jsonSerialilzerOptions))
                };
            });
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider);

        await tokenService.GetToken(); // Set initial token

        // Act
        var accessToken = await tokenService.GetToken();

        // Assert
        Assert.Equal(token.AccessToken, accessToken);
    }

    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(800)]
    [InlineData(1799)]
    [Theory]
    public async Task GetToken_WhenTokenExistsButIsNoLongerValid_ReturnsNewToken(int seconds)
    {
        // Arrange
        var currentTime = new DateTime(2025, 12, 5, 7, 20, 0).ToUniversalTime();
        _fakeTimeProvider.SetUtcNow(currentTime);

        long expiresInUnixTime = ((DateTimeOffset)currentTime).AddSeconds(seconds).ToUnixTimeSeconds();
        var token = new Token("FakeAccessToken", expiresInUnixTime);
        var token2 = new Token("FakeAccessToken2", expiresInUnixTime);
        var tokens = new[] { token, token2 };

        var callCount = 0;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(req =>
            {
                var token = tokens[callCount++];
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(token, _jsonSerialilzerOptions))
                };
            });
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider);

        await tokenService.GetToken(); // Set initial token

        // Act
        var accessToken = await tokenService.GetToken();

        // Assert
        Assert.Equal(token2.AccessToken, accessToken);
    }
}
