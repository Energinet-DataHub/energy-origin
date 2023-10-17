using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using NSubstitute.ReceivedExtensions;
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
    private readonly ICryptography cryptography = new Cryptography(new CryptographyOptions(){ Key = "secretsecretsecretsecret"});
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

    public static IEnumerable<object[]> BuildRedirectionUriParameters =>
       new List<object[]>
       {
            new object[] {new OidcState(null,null,null), OidcOptions.Redirection.Allow,"https://test.dk/"},
            new object[] {new OidcState(null, null, "/path/to/redirect"),OidcOptions.Redirection.Deny, "https://test.dk/?redirectionPath=path%2Fto%2Fredirect"},
            new object[] {new OidcState("someState", null, null), OidcOptions.Redirection.Deny, "https://test.dk/?state=someState"},
            new object[] {new OidcState("someState", "https://example.com/redirect", "/path/to/redirect"), OidcOptions.Redirection.Deny, "https://test.dk/?redirectionPath=path%2Fto%2Fredirect&state=someState"},
            new object[] {new OidcState("someState", "https://example.com/redirect", "/path/to/redirect"), OidcOptions.Redirection.Allow, "https://example.com/redirect?state=someState"},
            new object[] {new OidcState(null, "https://example.com/redirect", null), OidcOptions.Redirection.Allow, "https://example.com/redirect"}
       };

    [Theory]
    [MemberData(nameof(BuildRedirectionUriParameters))]
    public void BuildRedirectionUri_RerturnCorrectlyFormattedUri_WhenProvidedDifferentParamteres(OidcState? state, OidcOptions.Redirection redirection, string expectedUri)
    {
        var options = new OidcOptions() { RedirectionMode = redirection, FrontendRedirectUri = new Uri("https://test.dk") };
        var result = OidcController.OidcHelper.BuildRedirectionUri(options, state);

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

        var result = Assert.Throws<OidcController.RedirectionFlow>(() => OidcController.OidcHelper.TryVerifyCode(null, logger, error, errorDescription, "https://example.com/login?errorCode=714")).Url;

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
    public void CodeNullCheck_ShouldPass_WhenGivenCorrectConditions() => Assert.Null(Record.Exception(() => OidcController.OidcHelper.TryVerifyCode("code", logger, null, null, "https://example.com/login?errorCode=714")));

    #pragma warning disable 8625
    public static IEnumerable<object[]> WrongDiscoveryDocumentResponse =>
        new List<object[]>
        {
            new object[] {DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") })},
            new object[] {null}
        };
    #pragma warning restore 8625

    [Theory]
    [MemberData(nameof(WrongDiscoveryDocumentResponse))]
    public void TryVerifyDiscoveryDocument_ShouldFollowRedirectionFlow_WhenGivenErrorConditions(DiscoveryDocumentResponse? document)
    {
        var result = Assert.Throws<OidcController.RedirectionFlow>(() => OidcController.OidcHelper.TryVerifyDiscoveryDocument(document, logger, "https://test.dk")).Url;

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
    public void TryVerifyDiscoveryDocument_ShouldPass_WhenGivenCorrectConditions() => Assert.Null(Record.Exception(() => OidcController.OidcHelper.TryVerifyDiscoveryDocument(DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", "https://test.dk") }), logger, "https://test.dk")));


    [Fact]
    public void FetchTokenResponse_ShouldFollowRedirectionFlow_WhenRequestingTokenFails()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", tokenEndpoint.AbsoluteUri) });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{null}}", "id_token":"{{null}}", "userinfo_token":"{{null}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = Assert.ThrowsAsync<OidcController.RedirectionFlow>(() => OidcController.OidcHelper.FetchTokenResponse(factory, logger, oidcOptions, document, Guid.NewGuid().ToString(), "https://test.dk")).Result.Url;

        Assert.NotNull(result);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.BadResponse}", result);

        logger.Received(1).Log(
           Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
           Arg.Any<EventId>(),
           Arg.Any<object>(),
           Arg.Any<Exception>(),
           Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void FetchTokenResponse_ShouldReturnTokenResponse_WhenProvidedWithCorrectConditions()
    {
        var testOptions = TestOptions.Oidc(oidcOptions, reuseSubject: true);
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");
        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        var subject = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "scope", "some_scope" },
        });
        var name = Guid.NewGuid().ToString();
        var userInfoToken = TokenUsing(tokenOptions,
        document.Issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "mitid.uuid", Guid.NewGuid().ToString() },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userInfoToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = OidcController.OidcHelper.FetchTokenResponse(factory, logger, oidcOptions, document, Guid.NewGuid().ToString(), "https://example.com/login").Result;

        Assert.NotNull(result);
        Assert.IsType<TokenResponse>(result);

        Assert.Equal(tokenEndpoint.AbsoluteUri, result.HttpResponse.RequestMessage?.RequestUri?.AbsoluteUri);
        Assert.True(!result.IsError);

        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(identityToken, result.IdentityToken);
        Assert.Contains(userInfoToken, result.Raw);
    }

    [Fact]
    public void TryVerifyProviderType_ShouldThrow_WhenProviderTypeIsNotInProviderOptions()
    {
        var newOptions = new IdentityProviderOptions() { Providers = new List<ProviderType>() { ProviderType.MitIdPrivate, ProviderType.MitIdProfessional } };
        Assert.Throws<NotSupportedException>(() => OidcController.OidcHelper.TryVerifyProviderType(ProviderType.NemIdPrivate, newOptions));
    }
    [Fact]
    public void TryVerifyProviderType_ShouldPass_WhenProviderTypeIsInProviderOptions() => Assert.Null(Record.Exception(() => OidcController.OidcHelper.TryVerifyProviderType(ProviderType.NemIdPrivate, providerOptions)));

    [Theory]
    [InlineData(ProviderName.MitId, ProviderGroup.Private, ProviderType.MitIdPrivate)]
    [InlineData(ProviderName.MitIdProfessional, ProviderGroup.Professional, ProviderType.MitIdProfessional)]
    [InlineData(ProviderName.NemId, ProviderGroup.Private, ProviderType.NemIdPrivate)]
    [InlineData(ProviderName.NemId, ProviderGroup.Professional, ProviderType.NemIdProfessional)]
    public void GetIdentityProviderEnum_ShouldMatchCorrectProviderType_WhenProvidedWithVariousProviders(string providerName, string identity, ProviderType expected)
    {
        Assert.Equal(OidcController.OidcHelper.GetIdentityProviderEnum(providerName, identity), expected);
    }

    [Fact]
    public void GetIdentityProviderEnum_ShouldThrow_WhenProvidedWithWrongProviders() => Assert.Throws<NotImplementedException>(() => OidcController.OidcHelper.GetIdentityProviderEnum(ProviderName.MitIdProfessional, ProviderGroup.Private));

    public static IEnumerable<object[]> ValidUserInfoClaims =>
        new List<object[]>
        {
            new object[] {
                new UserInfoObject()
                {
                    ClaimsIdentity = new ClaimsIdentity(new List<Claim>
                    {
                        new("nemlogin.name", Guid.NewGuid().ToString()),
                        new("nemlogin.cvr", Guid.NewGuid().ToString()),
                        new("nemlogin.org_name", Guid.NewGuid().ToString()),
                        new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
                    }),
                    ProviderType = ProviderType.MitIdProfessional,
                    IdentityType = ProviderGroup.Professional,
                    ExpectedName = "nemlogin.name",
                    ExpectedTin = "nemlogin.cvr",
                    ExpectedCompanyName = "nemlogin.org_name",
                    ExpectedKeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Eia,"nemlogin.persistent_professional_id")}
                }
            },

            new object[] {
                new UserInfoObject()
                {
                    ClaimsIdentity = new ClaimsIdentity(new List<Claim>
                    {
                        new("nemlogin.name", Guid.NewGuid().ToString()),
                        new("nemlogin.cvr", Guid.NewGuid().ToString()),
                        new("nemlogin.org_name", Guid.NewGuid().ToString()),
                        new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                        new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString())
                    }),
                    ProviderType = ProviderType.MitIdProfessional,
                    IdentityType = ProviderGroup.Professional,
                    ExpectedName = "nemlogin.name",
                    ExpectedTin = "nemlogin.cvr",
                    ExpectedCompanyName = "nemlogin.org_name",
                    ExpectedKeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Eia,"nemlogin.persistent_professional_id"), (ProviderKeyType.Rid, "nemlogin.nemid.rid"), (ProviderKeyType.Rid, "nemlogin.cvr")}
                }
            },

            new object[] {
                new UserInfoObject()
                {
                    ClaimsIdentity = new ClaimsIdentity(new List<Claim>
                    {
                        new("mitid.uuid", Guid.NewGuid().ToString()),
                        new("mitid.identity_name", Guid.NewGuid().ToString()),
                    }),
                    ProviderType = ProviderType.MitIdPrivate,
                    IdentityType = ProviderGroup.Private,
                    ExpectedName = "mitid.identity_name",
                    ExpectedTin = "NotACompany",
                    ExpectedCompanyName = "NotACompany",
                    ExpectedKeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.MitIdUuid,"mitid.uuid")}
                }
            },

            new object[] {
                new UserInfoObject()
                {
                    ClaimsIdentity = new ClaimsIdentity(new List<Claim>
                    {
                        new("mitid.uuid", Guid.NewGuid().ToString()),
                        new("mitid.identity_name", Guid.NewGuid().ToString()),
                        new("nemid.pid", Guid.NewGuid().ToString()),
                    }),
                    ProviderType = ProviderType.MitIdPrivate,
                    IdentityType = ProviderGroup.Private,
                    ExpectedName = "mitid.identity_name",
                    ExpectedTin = "NotACompany",
                    ExpectedCompanyName = "NotACompany",
                    ExpectedKeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.MitIdUuid,"mitid.uuid"), (ProviderKeyType.Pid, "nemid.pid")}
                }
            },

            new object[] {
                new UserInfoObject()
                {
                    ClaimsIdentity = new ClaimsIdentity(new List<Claim>
                    {
                        new("nemid.common_name", Guid.NewGuid().ToString()),
                        new("nemid.cvr", Guid.NewGuid().ToString()),
                        new("nemid.company_name", Guid.NewGuid().ToString()),
                        new("nemid.ssn", Guid.NewGuid().ToString()),
                    }),
                    ProviderType = ProviderType.NemIdProfessional,
                    IdentityType = ProviderGroup.Professional,
                    ExpectedName = "nemid.common_name",
                    ExpectedTin = "nemid.cvr",
                    ExpectedCompanyName = "nemid.company_name",
                    ExpectedKeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Rid, "nemid.ssn")}
                }
            },

            new object[] {
                new UserInfoObject()
                {
                    ClaimsIdentity = new ClaimsIdentity(new List<Claim>
                    {
                        new("nemid.common_name", Guid.NewGuid().ToString()),
                        new("nemid.pid", Guid.NewGuid().ToString()),
                    }),
                    ProviderType = ProviderType.NemIdPrivate,
                    IdentityType = ProviderGroup.Private,
                    ExpectedName = "nemid.common_name",
                    ExpectedCompanyName = "NotACompany",
                    ExpectedTin = "NotACompany",
                    ExpectedKeyPairs = new List<(ProviderKeyType, string)>(){(ProviderKeyType.Pid, "nemid.pid")}
                }
            }
        };

    public class UserInfoObject
    {
        internal ClaimsIdentity ClaimsIdentity { get; init; } = new ClaimsIdentity();
        internal ProviderType ProviderType { get; init; }
        internal string IdentityType { get; init; } = string.Empty;
        internal string ExpectedName { get; init; } = string.Empty;
        internal string ExpectedTin { get; init; } = string.Empty;
        internal string ExpectedCompanyName { get; init; } = string.Empty;
        internal List<(ProviderKeyType dictionaryKeys, string claimsKeys)> ExpectedKeyPairs { get; init; } = new List<(ProviderKeyType, string)>();

    }
    [Theory]
    [MemberData(nameof(ValidUserInfoClaims))]
    public void HandleUserInfo_ShouldReturnCorrectUserinfo_WhenProvidedVariousParams(UserInfoObject infoObject)
    {
        var userInfoClaim = new ClaimsPrincipal();
        userInfoClaim.AddIdentity(infoObject.ClaimsIdentity);

        var (name, tin, companyName, keys) = OidcController.OidcHelper.HandleUserInfo(userInfoClaim, infoObject.ProviderType, infoObject.IdentityType);

        Assert.Equal(userInfoClaim.FindFirstValue(infoObject.ExpectedName), name);
        Assert.Equal(userInfoClaim.FindFirstValue(infoObject.ExpectedTin), tin);
        Assert.Equal(userInfoClaim.FindFirstValue(infoObject.ExpectedCompanyName), companyName);

        foreach (var (dictionaryKeys, claimsKeys) in infoObject.ExpectedKeyPairs)
        {
            Assert.True(keys.TryGetValue(dictionaryKeys, out string? value));
            Assert.Contains(userInfoClaim.FindFirstValue(claimsKeys)!, value);
        }
    }

    public static IEnumerable<object[]> wrongUserClaims =>
        new List<object[]>
        {
            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", Guid.NewGuid().ToString()),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(KeyNotFoundException)},

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
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", string.Empty),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
                new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },


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
                new("nemid.common_name", Guid.NewGuid().ToString()),
            }), ProviderType.NemIdPrivate, ProviderGroup.Private, typeof(KeyNotFoundException)},
        };
    [Theory]
    [MemberData(nameof(wrongUserClaims))]
    public void HandleUserInfo_ShouldThrow_WhenProvidedWithWrongParams(ClaimsIdentity claims, ProviderType providerType, string identityType, Type expectedException)
    {
        var userInfoClaim = new ClaimsPrincipal();
        userInfoClaim.AddIdentity(claims);

        Assert.Throws(expectedException, () => OidcController.OidcHelper.HandleUserInfo(userInfoClaim, providerType, identityType));
    }

    [Fact]
    public void FetchUserAsync_ShouldUpsertUser_WhenUserIsAlreadyKnown()
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

        var result = OidcController.OidcHelper.FetchUserAsync(service, userProviderService, userProviders, oidcOptions, "", "", "", "", "").Result;

        service.Received(1).UpsertUserAsync(Arg.Any<User>());

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
    }

    [Fact]
    public void FetchUserAsync_ShouldCreateNewUserWithCompany_WhenProviderGroupIsProfessional()
    {
        var newOptions = new OidcOptions(){IdGeneration = OidcOptions.Generation.Predictable};
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();

        var subjectId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var companyName = "Test_Company_Name";
        var tin = Guid.NewGuid().ToString();

        var result = OidcController.OidcHelper.FetchUserAsync(service, userProviderService, userProviders, newOptions, subjectId, ProviderGroup.Professional, name, tin, companyName).Result;

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        Assert.Equal(result.Name, name);

        Assert.NotNull(result.Company);
        Assert.Equal(result.Company.Id, Guid.Parse(subjectId));
        Assert.Equal(result.Company.Name, companyName);
        Assert.Equal(result.Company.Tin, tin);

        service.Received(0).UpsertUserAsync(Arg.Any<User>());
    }

    [Fact]
    public void FetchUserAsync_ShouldCreateNewUser_WhenProviderGroupIsPrivate()
    {
        var newOptions = new OidcOptions(){IdGeneration = OidcOptions.Generation.Predictable};
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();

        var subjectId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();

        var result = OidcController.OidcHelper.FetchUserAsync(service, userProviderService, userProviders, newOptions, subjectId, ProviderGroup.Private, name, Guid.NewGuid().ToString(), "DOES_NOT_HAVE_COMPANY").Result;

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        Assert.Equal(result.Name, name);
        Assert.Equal(result.Id, Guid.Parse(subjectId));

        Assert.Null(result.Company);
    }

    public static IEnumerable<object[]> RoleMatcherData =>
        new List<object[]>
        {
             new object[] {
                new ClaimsIdentity(new List<Claim>{new("test1","TESTADMIN")}),
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
                new List<string>{"TestRoleKeyAdmin","TestRoleKeyAdmin2","TestRoleKeyAdmin3","TestRoleKeyAdmin4"}
            },
            new object[] {
                new ClaimsIdentity(new List<Claim>{new("admin","TESTADMIN"), new("viewer","VIEWER_TO_BE_ADMIN")}),
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
            //REVIEW: Test FindFirstValue
            new object[] {
                new ClaimsIdentity(new List<Claim>{new("admin","TESTADMIN"), new("admin","VIEWER_TO_BE_ADMIN")}),
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
        var claims = new ClaimsPrincipal();
        claims.AddIdentity(identity);

        var options = new RoleOptions();
        options.RoleConfigurations.AddRange(roleConfigurations);

        var result = OidcController.OidcHelper.CalculateMatchedRoles(claims, options);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task MapUserDescriptor_ReturnsCorrectValuesBasedOfTokens_WhenOperationIsASuccess()
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

        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "scope", "some_scope" },
        });
        var name = Guid.NewGuid().ToString();
        var userInfoToken = TokenUsing(tokenOptions,
        document.Issuer, testOptions.ClientId, subject: subject, claims: new() {
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

        var result = await OidcController.OidcHelper.MapUserDescriptor(logger, cryptography, userProviderService, service, providerOptions, oidcOptions, roleOptions, document, tokenResponse, "https://example.com");

        Assert.IsType<(UserDescriptor, TokenIssuer.UserData)>(result);
        var (userDescriptor, _) = result;

        Assert.Equal(name, userDescriptor.Name);
        Assert.Equal(ProviderType.MitIdPrivate, userDescriptor.ProviderType);

        Assert.Equal(cryptography.Decrypt<string>(userDescriptor.EncryptedAccessToken), accessToken);
        Assert.Equal(cryptography.Decrypt<string>(userDescriptor.EncryptedIdentityToken), identityToken);

    }

    public static IEnumerable<object[]> ErrorTokens =>
        new List<object[]>
        {
            new object[] {
                new List<KeyValuePair<string, string>>(){
                    new("issuer", $"https://example.com/op"),
                    new("end_session_endpoint", $"http://example.com/connect/endsession")
                },
                new Dictionary<string, object?>() {
                    {"scope", "some_scope"}
                },
                new Dictionary<string, object?>() {
                    {"mitid.uuid", Guid.NewGuid().ToString()},
                    {"mitid.identity_name", Guid.NewGuid().ToString()},
                    {"idp", ProviderName.MitId},
                    {"identity_type", ProviderGroup.Private}
                },
                "The value cannot be an empty string. (Parameter 'subject')",
                "",
            },
            new object[] {
                new List<KeyValuePair<string, string>>(){
                    new("issuer", $"https://example.com/op"),
                    new("end_session_endpoint", $"http://example.com/connect/endsession")
                },
                new Dictionary<string, object?>() {
                    {"scope", "some_scope"}
                },
                new Dictionary<string, object?>() {
                    {"mitid.uuid", Guid.NewGuid().ToString()},
                    {"mitid.identity_name", Guid.NewGuid().ToString()},
                    {"idp", ProviderName.MitId},
                    {"identity_type", ProviderGroup.Private}
                },
                "Subject mismatched found in tokens received.",
                "subject", "IdentitySubject",
            },
            new object[] {
                 new List<KeyValuePair<string, string>>(){
                    new("issuer", $"https://example.com/op"),
                    new("end_session_endpoint", $"http://example.com/connect/endsession")
                },
                new Dictionary<string, object?>() {
                    {"scope", "some_scope"}
                },
                new Dictionary<string, object?>() {
                    {"mitid.uuid", Guid.NewGuid().ToString()},
                    {"mitid.identity_name", Guid.NewGuid().ToString()},
                    {"idp", ProviderName.MitId},
                    {"identity_type", ProviderGroup.Private}
                },
                "Subject mismatched found in tokens received.",
                "subject", "subject", "UserInfoSubject"
            },

            new object[] {
                new List<KeyValuePair<string, string>>(){
                    new("issuer", $"https://example.com/op"),
                    new("end_session_endpoint", $"http://example.com/connect/endsession")
                },
                new Dictionary<string, object?>() {
                    {"NOT_A_SCOPE", "NOT_A_SCOPE"}
                },
                new Dictionary<string, object?>() {
                    {"mitid.uuid", Guid.NewGuid().ToString()},
                    {"mitid.identity_name", Guid.NewGuid().ToString()},
                    {"idp", ProviderName.MitId},
                    {"identity_type", ProviderGroup.Private}
                },
                "Value cannot be null. (Parameter 'scope')",
            },

            new object[] {
                new List<KeyValuePair<string, string>>(){
                    new("issuer", $"https://example.com/op"),
                    new("end_session_endpoint", $"http://example.com/connect/endsession")
                },
                new Dictionary<string, object?>() {
                    {"scope", "some_scope"}
                },
                new Dictionary<string, object?>() {
                    {"mitid.uuid", Guid.NewGuid().ToString()},
                    {"mitid.identity_name", Guid.NewGuid().ToString()},
                    {"NOT_AN_IDP", "NOT_AN_IDP"},
                    {"identity_type", ProviderGroup.Private}
                },
                "Value cannot be null. (Parameter 'providerName')",
            },

            new object[] {
                new List<KeyValuePair<string, string>>(){
                    new("issuer", $"https://example.com/op"),
                    new("end_session_endpoint", $"http://example.com/connect/endsession")
                },
                new Dictionary<string, object?>() {
                    {"scope", "some_scope"}
                },
                new Dictionary<string, object?>() {
                    {"mitid.uuid", Guid.NewGuid().ToString()},
                    {"mitid.identity_name", Guid.NewGuid().ToString()},
                    {"idp", ProviderName.MitId},
                    {"NOT_AN_IDENTITYTYPE", "NOT_AN_IDENTITYTYPE"}
                },
                "Value cannot be null. (Parameter 'identityType')",
            },
        };

    //REVIEW: vi tester vel ikke validate token siden det er eksternt?
    //Kan ikke lave null check på subject pga. TokenUsing metoden
    [Theory]
    [MemberData(nameof(ErrorTokens))]
    public async Task MapUserDescriptor_FollowsRedirectionFlow_WhenProvidedWrongTokens(List<KeyValuePair<string, string>> documentItems, Dictionary<string, object> accessTokenClaims, Dictionary<string, object> userInfoTokenClaims, string expectedException, string? accessSubject = "subject", string? identitySubject = "subject", string? userInfoSubject = "subject")
    {
       var testOptions = TestOptions.Oidc(oidcOptions, reuseSubject: true);

        var document = DiscoveryDocument.Load(
            documentItems,
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: identitySubject);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: accessSubject, claims: accessTokenClaims);
        var userInfoToken = TokenUsing(tokenOptions,document.Issuer, testOptions.ClientId, subject: userInfoSubject, claims: userInfoTokenClaims);

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

        var result = await Assert.ThrowsAsync<OidcController.RedirectionFlow>(() => OidcController.OidcHelper.MapUserDescriptor(logger, cryptography, userProviderService, service, providerOptions, oidcOptions, roleOptions, document, tokenResponse, "https://example.com"));

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(exception => exception.Message == expectedException),
            Arg.Any<Func<object, Exception?, string>>()
        );

        Assert.Contains($"{ErrorCode.QueryString}%3D{ErrorCode.Authentication.InvalidTokens}", result.Url);
    }

    private static JsonWebKeySet KeySetUsing(byte[] pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(pem));
        var parameters = rsa.ExportParameters(false);

        var exponent = Base64Url.Encode(parameters.Exponent);
        var modulus = Base64Url.Encode(parameters.Modulus);
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
}
