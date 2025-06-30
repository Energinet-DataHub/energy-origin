using Xunit;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using Microsoft.Extensions.Time.Testing;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EnergyOrigin.Datahub3.Tests;

public class TokenServiceTests
{
    private readonly DataHub3Options _dataHub3Options;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly ILogger<TokenService> _logger;

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
        _logger = Substitute.For<ILogger<TokenService>>();
    }

    [Fact]
    public async Task GetToken_WhenMockEnabled_ReturnsGuid()
    {
        // Arrange
        var token = new Token("FakeAccessToken", 3600);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(HttpStatusCode.OK, new StringContent(JsonSerializer.Serialize(token, _jsonSerialilzerOptions)));
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        _dataHub3Options.EnableMock = true;
        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider, _logger);

        // Act
        var accessToken = await tokenService.GetToken();

        // Assert
        Assert.True(Guid.TryParse(accessToken, out _));
    }

    [Fact]
    public async Task GetToken_WhenNoTokenFetchedYet_ReturnsNewToken()
    {
        // Arrange
        var token = new Token("FakeAccessToken", 3600);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(HttpStatusCode.OK, new StringContent(JsonSerializer.Serialize(token, _jsonSerialilzerOptions)));
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider, _logger);

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

        var expiresIn = 3600;
        var token = new Token("FakeAccessToken", expiresIn);
        var token2 = new Token("FakeAccessToken2", expiresIn);
        var tokens = new[] { token, token2 };

        var callCount = 0;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(req =>
            {
                var selectedToken = tokens[callCount++];
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(selectedToken, _jsonSerialilzerOptions))
                };
            });
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider, _logger);

        await tokenService.GetToken(); // Fetch first token
        _fakeTimeProvider.SetUtcNow(currentTime.AddSeconds(expiresIn - seconds)); // Simulate passing time

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

        var expiresIn = 3600;
        var token = new Token("FakeAccessToken", expiresIn);
        var token2 = new Token("FakeAccessToken2", expiresIn);
        var tokens = new[] { token, token2 };

        var callCount = 0;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(_dataHub3Options.TokenUrl!)
            .Respond(req =>
            {
                var selectedToken = tokens[callCount++];
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(selectedToken, _jsonSerialilzerOptions))
                };
            });
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        var options = Options.Create(_dataHub3Options);
        var tokenService = new TokenService(httpClient, options, _fakeTimeProvider, _logger);

        await tokenService.GetToken(); // Set initial token
        _fakeTimeProvider.SetUtcNow(currentTime.AddSeconds(expiresIn - seconds)); // Simulate passing time

        // Act
        var accessToken = await tokenService.GetToken();

        // Assert
        Assert.Equal(token2.AccessToken, accessToken);
    }
}
