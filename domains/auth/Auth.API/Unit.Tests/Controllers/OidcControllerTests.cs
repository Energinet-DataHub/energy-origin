using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;
using static API.Options.RoleConfiguration;
using JsonWebKeySet = IdentityModel.Jwk.JsonWebKeySet;

namespace Unit.Tests.Controllers;

public class OidcControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly TokenOptions tokenOptions;
    private readonly IdentityProviderOptions providerOptions;
    private readonly RoleOptions roleOptions;
    private readonly ICryptography cryptography = new Cryptography(new CryptographyOptions() { Key = "secretsecretsecretsecret" });
    private readonly IUserService service = Substitute.For<IUserService>();
    private readonly IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
    private readonly IUserProviderService userProviderService = Substitute.For<IUserProviderService>();
    private readonly ILogger<OidcController> logger = Substitute.For<ILogger<OidcController>>();
    private readonly MockHttpMessageHandler http = new();

    public OidcControllerTests()
    {
        IdentityModelEventSource.ShowPII = true;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
        tokenOptions = configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!;
        providerOptions = configuration.GetSection(IdentityProviderOptions.Prefix).Get<IdentityProviderOptions>()!;
        roleOptions = configuration.GetSection(RoleOptions.Prefix).Get<RoleOptions>()!;
    }

    public static IEnumerable<object[]> BuildRedirectionUriParameters => new List<object[]>
    {
        new object[] {new OidcState(null,null,null), OidcOptions.Redirection.Allow,"https://localhost/"},
        new object[] {new OidcState(null, null, "/path/to/redirect"), OidcOptions.Redirection.Deny, "https://localhost/?redirectionPath=path%2Fto%2Fredirect"},
        new object[] {new OidcState("someState", null, null), OidcOptions.Redirection.Deny, "https://localhost/?state=someState"},
        new object[] {new OidcState("someState", "https://example.com/redirect", "/path/to/redirect"), OidcOptions.Redirection.Deny, "https://localhost/?redirectionPath=path%2Fto%2Fredirect&state=someState"},
        new object[] {new OidcState("someState", "https://example.com/redirect", "/path/to/redirect"), OidcOptions.Redirection.Allow, "https://example.com/redirect?state=someState"},
        new object[] {new OidcState(null, "https://example.com/redirect", null), OidcOptions.Redirection.Allow, "https://example.com/redirect"}
    };

    [Theory]
    [MemberData(nameof(BuildRedirectionUriParameters))]
    public void BuildRedirectionUri_ReturnCorrectlyFormattedUri_WhenProvidedDifferentParameters(OidcState? state, OidcOptions.Redirection redirection, string expectedUri)
    {
        var options = new OidcOptions() { RedirectionMode = redirection, FrontendRedirectUri = new Uri("https://localhost") };

        var result = OidcHelper.BuildRedirectionUri(options, state);
        Assert.Equal(expectedUri, result);
    }

    [Theory]
    [InlineData("access_denied", "internal_error", ErrorCode.AuthenticationUpstream.InternalError)]
    [InlineData("access_denied", "user_aborted", ErrorCode.AuthenticationUpstream.Aborted)]
    [InlineData("access_denied", "private_to_business_user_aborted", ErrorCode.AuthenticationUpstream.Aborted)]
    [InlineData("access_denied", "no_ctx", ErrorCode.AuthenticationUpstream.NoContext)]
    [InlineData("access_denied", null, ErrorCode.AuthenticationUpstream.Failed)]
    [InlineData("invalid_request", null, ErrorCode.AuthenticationUpstream.InvalidRequest)]
    [InlineData("unauthorized_client", null, ErrorCode.AuthenticationUpstream.InvalidClient)]
    [InlineData("unsupported_response_type", null, ErrorCode.AuthenticationUpstream.InvalidRequest)]
    [InlineData("invalid_scope", null, ErrorCode.AuthenticationUpstream.InvalidScope)]
    [InlineData("server_error", null, ErrorCode.AuthenticationUpstream.InternalError)]
    [InlineData("temporarily_unavailable", null, ErrorCode.AuthenticationUpstream.InternalError)]
    [InlineData(null, null, ErrorCode.AuthenticationUpstream.Failed)]
    public void TryVerifyCode_ShouldFollowRedirectionFlow_WhenGivenErrorConditions(string? error, string? errorDescription, string expected)
    {

        var result = Assert.Throws<OidcHelper.RedirectionFlow>(() => OidcHelper.TryVerifyCode(null, logger, error, errorDescription, "https://example.com/login?errorCode=714")).Url;

        Assert.NotNull(result);
        Assert.Contains($"{ErrorCode.QueryString}={expected}", result);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public void CodeNullCheck_ShouldPass_WhenGivenCorrectConditions() => Assert.True(DoesNotThrow(() => OidcHelper.TryVerifyCode("code", logger, null, null, "https://example.com/login?errorCode=714")));

    public static IEnumerable<object[]> WrongDiscoveryDocumentResponse => new List<object[]>
    {
        new object[] {DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") })},
        new object[] {null!}
    };

    [Theory]
    [MemberData(nameof(WrongDiscoveryDocumentResponse))]
    public void TryVerifyDiscoveryDocument_ShouldFollowRedirectionFlow_WhenGivenErrorConditions(DiscoveryDocumentResponse? document)
    {
        var result = Assert.Throws<OidcHelper.RedirectionFlow>(() => OidcHelper.TryVerifyDiscoveryDocument(document, logger, "https://example.com")).Url;

        Assert.NotNull(result);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", result);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TryVerifyDiscoveryDocument_ShouldPass_WhenGivenCorrectConditions() => Assert.True(DoesNotThrow(() => OidcHelper.TryVerifyDiscoveryDocument(DiscoveryDocument.Load(Enumerable.Empty<KeyValuePair<string, string>>()), logger, "https://example.com")));

    [Fact]
    public async Task FetchTokenResponse_ShouldFollowRedirectionFlow_WhenRequestingTokenFails()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri) });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond(HttpStatusCode.NotFound);

        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = await Assert.ThrowsAsync<OidcHelper.RedirectionFlow>(() => OidcHelper.FetchTokenResponse(factory, logger, oidcOptions, document, Guid.NewGuid().ToString(), "https://example.com"));

        Assert.NotNull(result.Url);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.BadResponse}", result.Url);

        logger.Received(1).Log(
           Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
           Arg.Any<EventId>(),
           Arg.Any<object>(),
           Arg.Any<Exception>(),
           Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task FetchTokenResponse_ShouldReturnTokenResponse_WhenProvidedWithCorrectConditions()
    {
        var testOptions = TestOptions.Oidc(oidcOptions, reuseSubject: true);
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");
        var issuer = "issuer";
        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        var subject = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, issuer, testOptions.ClientId, subject: subject);
        var accessToken = TokenUsing(tokenOptions, issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "scope", "some_scope" },
        });
        var name = Guid.NewGuid().ToString();
        var userInfoToken = TokenUsing(tokenOptions, issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "mitid.uuid", Guid.NewGuid().ToString() },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userInfoToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = await OidcHelper.FetchTokenResponse(factory, logger, oidcOptions, document, Guid.NewGuid().ToString(), "https://example.com/login");

        Assert.NotNull(result);
        Assert.IsType<TokenResponse>(result);

        Assert.Equal(tokenEndpoint.AbsoluteUri, result.HttpResponse.RequestMessage?.RequestUri?.AbsoluteUri);
        Assert.False(result.IsError);

        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(identityToken, result.IdentityToken);
        Assert.Equal(userInfoToken, result.TryGet("userinfo_token"));
    }

    public static IEnumerable<object[]> WrongProviderOptions => new List<object[]>
    {
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdProfessional }},
            ProviderType.NemIdPrivate
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.NemIdProfessional
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.NemIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.MitIdProfessional
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdProfessional, ProviderType.NemIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.MitIdPrivate
        }
    };

    [Theory]
    [MemberData(nameof(WrongProviderOptions))]
    public void TryVerifyProviderType_ShouldThrow_WhenProviderTypeIsNotInProviderOptions(IdentityProviderOptions options, ProviderType providerType)
    {
        Assert.Throws<NotSupportedException>(() => OidcHelper.TryVerifyProviderType(providerType, options));
    }

    public static IEnumerable<object[]> CorrectProviderOptions => new List<object[]>
    {
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.NemIdPrivate
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.NemIdProfessional
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.MitIdProfessional
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() {  ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdProfessional, ProviderType.NemIdPrivate }},
            ProviderType.MitIdPrivate
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.NemIdPrivate, ProviderType.NemIdPrivate, ProviderType.NemIdPrivate, ProviderType.NemIdPrivate }},
            ProviderType.NemIdPrivate
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.NemIdPrivate }},
            ProviderType.NemIdPrivate
        },
        new object[] {
            new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdPrivate, ProviderType.MitIdPrivate, ProviderType.NemIdPrivate }},
            ProviderType.NemIdPrivate
        },
    };

    [Theory]
    [MemberData(nameof(CorrectProviderOptions))]
    public void TryVerifyProviderType_ShouldPadss_WhenProviderTypeIsInProviderOptions(IdentityProviderOptions options, ProviderType providerType) => Assert.True(DoesNotThrow(() => OidcHelper.TryVerifyProviderType(providerType, options)));

    [Theory]
    [InlineData(ProviderName.MitId, ProviderGroup.Private, ProviderType.MitIdPrivate)]
    [InlineData(ProviderName.MitIdProfessional, ProviderGroup.Professional, ProviderType.MitIdProfessional)]
    [InlineData(ProviderName.NemId, ProviderGroup.Private, ProviderType.NemIdPrivate)]
    [InlineData(ProviderName.NemId, ProviderGroup.Professional, ProviderType.NemIdProfessional)]
    public void MatchIdentityProviderEnum_ShouldMatchCorrectProviderType_WhenProvidedWithVariousProviders(string providerName, string identity, ProviderType expected) => Assert.Equal(OidcHelper.MatchIdentityProviderEnum(providerName, identity), expected);

    [Fact]
    public void MatchIdentityProviderEnum_ShouldThrow_WhenProvidedWithWrongProviders() => Assert.Throws<NotImplementedException>(() => OidcHelper.MatchIdentityProviderEnum(ProviderName.MitIdProfessional, ProviderGroup.Private));

    public class UserInfo
    {
        internal ClaimsIdentity Identity { get; init; } = new ClaimsIdentity();
        internal ProviderType ProviderType { get; init; }
        internal string IdentityType { get; init; } = string.Empty;
        internal ExpectedValues Expected { get; init; } = new ExpectedValues();

        public class ExpectedValues
        {
            internal string Name { get; init; } = string.Empty;
            internal string? Tin { get; init; } = string.Empty;
            internal string? CompanyName { get; init; } = string.Empty;
            internal List<(ProviderKeyType dictionaryKeys, string claimsKeys)> KeyPairs { get; init; } = new List<(ProviderKeyType, string)>();
        }
    }
    public static IEnumerable<object[]> ValidUserInfoClaims => new List<object[]>
    {
        new object[] {
            new UserInfo()
            {
                Identity = new ClaimsIdentity(new List<Claim>
                {
                    new("nemlogin.name", Guid.NewGuid().ToString()),
                    new("nemlogin.cvr", Guid.NewGuid().ToString()),
                    new("nemlogin.org_name", Guid.NewGuid().ToString()),
                    new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
                }),
                ProviderType = ProviderType.MitIdProfessional,
                IdentityType = ProviderGroup.Professional,
                Expected = new UserInfo.ExpectedValues()
                {
                    Name = "nemlogin.name",
                    Tin = "nemlogin.cvr",
                    CompanyName = "nemlogin.org_name",
                    KeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Eia,"nemlogin.persistent_professional_id")}
                }
            }
        },

        new object[] {
            new UserInfo()
            {
                Identity = new ClaimsIdentity(new List<Claim>
                {
                    new("nemlogin.name", Guid.NewGuid().ToString()),
                    new("nemlogin.cvr", Guid.NewGuid().ToString()),
                    new("nemlogin.org_name", Guid.NewGuid().ToString()),
                    new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                    new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString())
                }),
                ProviderType = ProviderType.MitIdProfessional,
                IdentityType = ProviderGroup.Professional,
                Expected = new UserInfo.ExpectedValues()
                {
                    Name = "nemlogin.name",
                    Tin = "nemlogin.cvr",
                    CompanyName = "nemlogin.org_name",
                    KeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Eia, "nemlogin.persistent_professional_id"), (ProviderKeyType.Rid, "nemlogin.nemid.rid"), (ProviderKeyType.Rid, "nemlogin.cvr")}
                }
            }
        },

        new object[] {
            new UserInfo()
            {
                Identity = new ClaimsIdentity(new List<Claim>
                {
                    new("mitid.uuid", Guid.NewGuid().ToString()),
                    new("mitid.identity_name", Guid.NewGuid().ToString()),
                }),
                ProviderType = ProviderType.MitIdPrivate,
                IdentityType = ProviderGroup.Private,
                Expected = new UserInfo.ExpectedValues()
                {
                    Name = "mitid.identity_name",
                    Tin = null,
                    CompanyName = null,
                    KeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.MitIdUuid,"mitid.uuid")}
                }
            }
        },

        new object[] {
            new UserInfo()
            {
                Identity = new ClaimsIdentity(new List<Claim>
                {
                    new("mitid.uuid", Guid.NewGuid().ToString()),
                    new("mitid.identity_name", Guid.NewGuid().ToString()),
                    new("nemid.pid", Guid.NewGuid().ToString()),
                }),
                ProviderType = ProviderType.MitIdPrivate,
                IdentityType = ProviderGroup.Private,
                Expected = new UserInfo.ExpectedValues()
                {
                    Name = "mitid.identity_name",
                    Tin = null,
                    CompanyName = null,
                    KeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.MitIdUuid,"mitid.uuid"), (ProviderKeyType.Pid, "nemid.pid")}
                }
            }
        },

        new object[] {
            new UserInfo()
            {
                Identity = new ClaimsIdentity(new List<Claim>
                {
                    new("nemid.common_name", Guid.NewGuid().ToString()),
                    new("nemid.cvr", Guid.NewGuid().ToString()),
                    new("nemid.company_name", Guid.NewGuid().ToString()),
                    new("nemid.ssn", Guid.NewGuid().ToString()),
                }),
                ProviderType = ProviderType.NemIdProfessional,
                IdentityType = ProviderGroup.Professional,
                Expected = new UserInfo.ExpectedValues()
                {
                    Name = "nemid.common_name",
                    Tin = "nemid.cvr",
                    CompanyName = "nemid.company_name",
                    KeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Rid, "nemid.ssn")}
                }
            }
        },

        new object[] {
            new UserInfo()
            {
                Identity = new ClaimsIdentity(new List<Claim>
                {
                    new("nemid.common_name", Guid.NewGuid().ToString()),
                    new("nemid.pid", Guid.NewGuid().ToString()),
                }),
                ProviderType = ProviderType.NemIdPrivate,
                IdentityType = ProviderGroup.Private,
                Expected = new UserInfo.ExpectedValues()
                {
                    Name = "nemid.common_name",
                    Tin = null,
                    CompanyName = null,
                    KeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Pid, "nemid.pid")}
                }
            }
        }
    };

    [Theory]
    [MemberData(nameof(ValidUserInfoClaims))]
    public void ExtractUserInfo_ShouldReturnCorrectUserinfo_WhenProvidedVariousParams(UserInfo userInfo)
    {
        var claims = new ClaimsPrincipal(userInfo.Identity);

        var (name, tin, companyName, keys) = OidcHelper.ExtractUserInfo(claims, userInfo.ProviderType, userInfo.IdentityType);

        Assert.Equal(claims.FindFirstValue(userInfo.Expected.Name), name);
        Assert.Equal(claims.FindFirstValue(userInfo.Expected.Tin ?? string.Empty), tin);
        Assert.Equal(claims.FindFirstValue(userInfo.Expected.CompanyName ?? string.Empty), companyName);

        foreach (var (dictionaryKeys, claimsKeys) in userInfo.Expected.KeyPairs)
        {
            Assert.True(keys.TryGetValue(dictionaryKeys, out string? value));
            Assert.Contains(claims.FindFirstValue(claimsKeys)!, value);
        }
    }

    public static IEnumerable<object[]> WrongUserClaims => new List<object[]>
    {
        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.name", Guid.NewGuid().ToString()),
            new("nemlogin.cvr", Guid.NewGuid().ToString()),
            new("nemlogin.org_name", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(KeyNotFoundException)},

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.name", string.Empty),
            new("nemlogin.org_name", Guid.NewGuid().ToString()),
            new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
            new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.org_name", Guid.NewGuid().ToString()),
            new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
            new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.name", Guid.NewGuid().ToString()),
            new("nemlogin.org_name", Guid.NewGuid().ToString()),
            new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
            new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

         new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.name", Guid.NewGuid().ToString()),
            new("nemlogin.cvr", string.Empty),
            new("nemlogin.org_name", Guid.NewGuid().ToString()),
            new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
            new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.name", Guid.NewGuid().ToString()),
            new("nemlogin.cvr", Guid.NewGuid().ToString()),
            new("nemlogin.org_name",  string.Empty),
            new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
            new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemlogin.name", Guid.NewGuid().ToString()),
            new("nemlogin.cvr", Guid.NewGuid().ToString()),
            new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
            new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("mitid.identity_name", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdPrivate, ProviderGroup.Private, typeof(KeyNotFoundException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("mitid.uuid", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdPrivate, ProviderGroup.Private, typeof(ArgumentNullException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("mitid.identity_name", string.Empty),
            new("mitid.uuid", Guid.NewGuid().ToString()),
        }), ProviderType.MitIdPrivate, ProviderGroup.Private, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", Guid.NewGuid().ToString()),
            new("nemid.cvr", Guid.NewGuid().ToString()),
            new("nemid.company_name", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(KeyNotFoundException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", string.Empty),
            new("nemid.cvr", Guid.NewGuid().ToString()),
            new("nemid.company_name", Guid.NewGuid().ToString()),
            new("nemid.ssn", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.cvr", Guid.NewGuid().ToString()),
            new("nemid.company_name", Guid.NewGuid().ToString()),
            new("nemid.ssn", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", Guid.NewGuid().ToString()),
            new("nemid.cvr", string.Empty),
            new("nemid.company_name", Guid.NewGuid().ToString()),
            new("nemid.ssn", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", Guid.NewGuid().ToString()),
            new("nemid.company_name", Guid.NewGuid().ToString()),
            new("nemid.ssn", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", Guid.NewGuid().ToString()),
            new("nemid.cvr", Guid.NewGuid().ToString()),
            new("nemid.company_name", string.Empty),
            new("nemid.ssn", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", Guid.NewGuid().ToString()),
            new("nemid.cvr", Guid.NewGuid().ToString()),
            new("nemid.ssn", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.common_name", string.Empty),
            new("nemid.pid", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdPrivate, ProviderGroup.Private, typeof(ArgumentException)},

        new object[] {new ClaimsIdentity(new List<Claim>
        {
            new("nemid.pid", Guid.NewGuid().ToString()),
        }), ProviderType.NemIdPrivate, ProviderGroup.Private, typeof(ArgumentNullException)},
    };

    [Theory]
    [MemberData(nameof(WrongUserClaims))]
    public void ExtractUserInfo_ShouldThrow_WhenProvidedWithWrongParams(ClaimsIdentity identity, ProviderType providerType, string identityType, Type expectedException) => Assert.Throws(expectedException, () => OidcHelper.ExtractUserInfo(new ClaimsPrincipal(identity), providerType, identityType));

    [Fact]
    public void FetchOrCreateUserAndUpdateUserProvidersAsync_ShouldUpsertUser_WhenUserIsAlreadyKnown()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        service.GetUserByIdAsync(Arg.Any<Guid?>()).Returns(new User
        {
            Id = id,
            Name = name,
            UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 1 } },
            AllowCprLookup = true
        });
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();

        var result = OidcHelper.FetchOrCreateUserAndUpdateUserProvidersAsync(service, userProviderService, userProviders, oidcOptions, "", "", "", "", "").Result;

        service.Received(1).UpsertUserAsync(Arg.Any<User>());

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
    }

    [Fact]
    public async Task FetchOrCreateUserAndUpdateUserProvidersAsync_ShouldCreateNewUserWithCompany_WhenProviderGroupIsProfessional()
    {
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();

        var name = Guid.NewGuid().ToString();
        var companyName = "Test_Company_Name";
        var tin = Guid.NewGuid().ToString();

        var result = await OidcHelper.FetchOrCreateUserAndUpdateUserProvidersAsync(service, userProviderService, userProviders, oidcOptions, Guid.NewGuid().ToString(), ProviderGroup.Professional, name, tin, companyName);

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        Assert.Equal(result.Name, name);

        Assert.NotNull(result.Company);
        Assert.Equal(result.Company.Name, companyName);
        Assert.Equal(result.Company.Tin, tin);

        await service.Received(0).UpsertUserAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task FetchOrCreateUserAndUpdateUserProvidersAsync_ShouldCreateNewUser_WhenProviderGroupIsPrivate()
    {
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();
        var name = Guid.NewGuid().ToString();

        var result = await OidcHelper.FetchOrCreateUserAndUpdateUserProvidersAsync(service, userProviderService, userProviders, oidcOptions, Guid.NewGuid().ToString(), ProviderGroup.Private, name, Guid.NewGuid().ToString(), "DOES_NOT_HAVE_COMPANY");

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        Assert.Equal(result.Name, name);

        Assert.Null(result.Company);

        await service.Received(0).UpsertUserAsync(Arg.Any<User>());
    }

    public static IEnumerable<object[]> RoleMatcherData => new List<object[]>
    {
        new object[]
        {
            new ClaimsIdentity(new List<Claim>{new("test1", "TESTADMIN")}),
            new List<RoleConfiguration>{
                new (){
                    Key = "TestRoleKeyAdmin",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new (){
                            Property = "test1",
                            Value = "ADMIN",
                            Operator = "contains"},
                    }
                },
                new (){
                    Key = "TestRoleKeyAdmin2",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new (){
                            Property = "test1",
                            Value = "TESTADMIN",
                            Operator = "equals"},
                    }
                },
                new (){
                    Key = "TestRoleKeyAdmin3",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new(){
                            Property = "test1",
                            Value = "beep_boop_test",
                            Operator = "exists"},
                    }
                },
                new (){
                    Key = "TestRoleKeyAdmin4",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new (){
                            Property = "test1",
                            Value = "boop_beep",
                            Operator = "equals"},
                        new(){
                            Property = "test1",
                            Value = "TEST",
                            Operator = "contains"},
                    }
                },
                new (){
                    Key = "WrongTestRoleKey",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new(){
                            Property = "test1",
                            Value = "TESTADMIN",
                            Operator = "wrong_operator"},
                    }
                },
            },
            new List<string>{"TestRoleKeyAdmin", "TestRoleKeyAdmin2", "TestRoleKeyAdmin3", "TestRoleKeyAdmin4"}
        },
        new object[] {
            new ClaimsIdentity(new List<Claim>{new("admin", "TESTADMIN"), new("viewer", "VIEWER_TO_BE_ADMIN")}),
            new List<RoleConfiguration>{
                new(){
                    Key = "TestRoleKeyAdmin1",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new (){
                            Property = "viewer",
                            Value = "VIEWER_TO_BE_ADMIN",
                            Operator = "equals"},
                        new(){
                            Property = "admin",
                            Value = "ADMIN",
                            Operator = "contains"},
                    }
                }
            },
            new List<string>{"TestRoleKeyAdmin1"}
        },
        new object[] {
            new ClaimsIdentity(new List<Claim>{new("admin", "TESTADMIN"), new("admin", "VIEWER_TO_BE_ADMIN")}),
            new List<RoleConfiguration>{
                new(){
                    Key = "TestRoleKeyAdmin1",
                    Name = "TestAdmin",
                    Matches = new List<Match>(){
                        new (){
                            Property = "admin",
                            Value = "VIEWER_TO_BE_ADMIN",
                            Operator = "equals"},
                        new(){
                            Property = "admin",
                            Value = "ADMIN",
                            Operator = "contains"},
                    }
                }
            },
            new List<string>{"TestRoleKeyAdmin1"}
        }
    };

    [Theory]
    [MemberData(nameof(RoleMatcherData))]
    public void CalculateMatchedRoles_ShouldReturnCorrectCollectionOfMatchedRoles_WhenDifferentConfigurationsIsProvided(ClaimsIdentity identity, List<RoleConfiguration> roleConfigurations, List<string> expected)
    {
        var claims = new ClaimsPrincipal(identity);
        var options = new RoleOptions() { RoleConfigurations = roleConfigurations };

        var result = OidcHelper.CalculateMatchedRoles(claims, options);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task BuildUserDescriptor_ReturnsCorrectValuesBasedOnTokens_WhenOperationIsASuccess()
    {
        var testOptions = TestOptions.Oidc(oidcOptions, reuseSubject: true);
        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{testOptions.AuthorityUri.Host}/op"),
                new("end_session_endpoint", $"http://{testOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        var subject = Guid.NewGuid().ToString();

        var identityToken = TokenUsing(tokenOptions, document.Issuer!, testOptions.ClientId, subject: subject);
        var accessToken = TokenUsing(tokenOptions, document.Issuer!, testOptions.ClientId, subject: subject, claims: new() {
            { "scope", "some_scope" },
        });
        var name = Guid.NewGuid().ToString();
        var userInfoToken = TokenUsing(tokenOptions,
        document.Issuer!, testOptions.ClientId, subject: subject, claims: new() {
            { "mitid.uuid", Guid.NewGuid().ToString() },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private }
        });

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                access_token = accessToken,
                id_token = identityToken,
                userinfo_token = userInfoToken,
            })
        };
        var tokenResponse = await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(httpResponseMessage);

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var result = await OidcHelper.BuildUserDescriptor(logger, cryptography, userProviderService, service, providerOptions, oidcOptions, roleOptions, document, tokenResponse, "https://example.com");

        Assert.IsType<(UserDescriptor, TokenIssuer.UserData)>(result);
        var (userDescriptor, _) = result;

        Assert.Equal(name, userDescriptor.Name);
        Assert.Equal(ProviderType.MitIdPrivate, userDescriptor.ProviderType);

        Assert.Equal(cryptography.Decrypt<string>(userDescriptor.EncryptedAccessToken), accessToken);
        Assert.Equal(cryptography.Decrypt<string>(userDescriptor.EncryptedIdentityToken), identityToken);
    }


    public static IEnumerable<object[]> ErrorTokens => new List<object[]>
    {
        new object[] {
            new Dictionary<string, object?>() {
                {"scope", "some_scope"}
            },
            new Dictionary<string, object?>() {
                {"mitid.uuid", Guid.NewGuid().ToString()},
                {"mitid.identity_name", Guid.NewGuid().ToString()},
                {"idp", ProviderName.MitId},
                {"identity_type", ProviderGroup.Private}
            },
            "",
        },
        new object[] {
            new Dictionary<string, object?>() {
                {"scope", "some_scope"}
            },
            new Dictionary<string, object?>() {
                {"mitid.uuid", Guid.NewGuid().ToString()},
                {"mitid.identity_name", Guid.NewGuid().ToString()},
                {"idp", ProviderName.MitId},
                {"identity_type", ProviderGroup.Private}
            },
            "subject", "IdentitySubject",
        },
        new object[] {
            new Dictionary<string, object?>() {
                {"scope", "some_scope"}
            },
            new Dictionary<string, object?>() {
                {"mitid.uuid", Guid.NewGuid().ToString()},
                {"mitid.identity_name", Guid.NewGuid().ToString()},
                {"idp", ProviderName.MitId},
                {"identity_type", ProviderGroup.Private}
            },
            "subject", "subject", "UserInfoSubject"
        },

        new object[] {
            new Dictionary<string, object?>() {
                {"NOT_A_SCOPE", "NOT_A_SCOPE"}
            },
            new Dictionary<string, object?>() {
                {"mitid.uuid", Guid.NewGuid().ToString()},
                {"mitid.identity_name", Guid.NewGuid().ToString()},
                {"idp", ProviderName.MitId},
                {"identity_type", ProviderGroup.Private}
            },
        },

        new object[] {
            new Dictionary<string, object?>() {
                {"scope", "some_scope"}
            },
            new Dictionary<string, object?>() {
                {"mitid.uuid", Guid.NewGuid().ToString()},
                {"mitid.identity_name", Guid.NewGuid().ToString()},
                {"NOT_AN_IDP", "NOT_AN_IDP"},
                {"identity_type", ProviderGroup.Private}
            },
        },

        new object[] {
            new Dictionary<string, object?>() {
                {"scope", "some_scope"}
            },
            new Dictionary<string, object?>() {
                {"mitid.uuid", Guid.NewGuid().ToString()},
                {"mitid.identity_name", Guid.NewGuid().ToString()},
                {"idp", ProviderName.MitId},
                {"NOT_AN_IDENTITYTYPE", "NOT_AN_IDENTITYTYPE"}
            },
        },
    };

    [Theory]
    [MemberData(nameof(ErrorTokens))]
    public async Task BuildUserDescriptor_FollowsRedirectionFlow_WhenProvidedWrongTokens(Dictionary<string, object> accessTokenClaims, Dictionary<string, object> userInfoTokenClaims, string? accessSubject = "subject", string? identitySubject = "subject", string? userInfoSubject = "subject")
    {
        var testOptions = TestOptions.Oidc(oidcOptions, reuseSubject: true);

        var document = DiscoveryDocument.Load(
             new List<KeyValuePair<string, string>>(){
                new("issuer", $"https://example.com/op"),
                new("end_session_endpoint", $"http://example.com/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        var identityToken = TokenUsing(tokenOptions, document.Issuer!, testOptions.ClientId, subject: identitySubject);
        var accessToken = TokenUsing(tokenOptions, document.Issuer!, testOptions.ClientId, subject: accessSubject, claims: accessTokenClaims);
        var userInfoToken = TokenUsing(tokenOptions, document.Issuer!, testOptions.ClientId, subject: userInfoSubject, claims: userInfoTokenClaims);

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                access_token = accessToken,
                id_token = identityToken,
                userinfo_token = userInfoToken,
            })
        };
        var tokenResponse = await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(httpResponseMessage);

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var result = await Assert.ThrowsAsync<OidcHelper.RedirectionFlow>(() => OidcHelper.BuildUserDescriptor(logger, cryptography, userProviderService, service, providerOptions, oidcOptions, roleOptions, document, tokenResponse, "https://example.com"));

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );

        var logoutUri = HttpUtility.ParseQueryString(new Uri(result.Url).Query).Get("post_logout_redirect_uri");
        Assert.NotNull(logoutUri);

        var query = HttpUtility.ParseQueryString(new Uri(logoutUri).Query).Get(ErrorCode.QueryString);

        Assert.Equal(ErrorCode.Authentication.InvalidTokens, query);
    }

    private static JsonWebKeySet KeySetUsing(byte[] pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(pem));
        var parameters = rsa.ExportParameters(false);

        var exponent = Base64Url.Encode(parameters.Exponent!);
        var modulus = Base64Url.Encode(parameters.Modulus!);
        var kid = SHA256.HashData(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(new Dictionary<string, string>() {
            {"e", exponent},
            {"kty", "RSA"},
            {"n", modulus}
        })));

        return new JsonWebKeySet
        {
            Keys = new() { new() {
                Kid = Base64Url.Encode(kid),
                Kty = "RSA",
                E = exponent,
                N = modulus
            }}
        };
    }

    private static string TokenUsing(TokenOptions tokenOptions, string issuer, string audience, string? subject = default, Dictionary<string, object>? claims = default)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(tokenOptions.PrivateKeyPem));
        var key = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var updatedClaims = claims ?? new Dictionary<string, object>();
        updatedClaims.Add("sub", subject ?? "subject");

        var descriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = credentials,
            Claims = updatedClaims
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }

    private static bool DoesNotThrow(Action action)
    {
        try
        {
            action.Invoke();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
