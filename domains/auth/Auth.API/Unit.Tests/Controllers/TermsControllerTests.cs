using System.Net;
using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Unit.Tests.Controllers;

public class TermsControllerTests
{
    // FIXME: add more testfullness if you have time

    private readonly TermsController termsController = new();
    private readonly ILogger<TermsController> logger = Mock.Of<ILogger<TermsController>>();
    private readonly IHttpContextAccessor accessor = Mock.Of<IHttpContextAccessor>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly IHttpClientFactory factory = Mock.Of<IHttpClientFactory>();
    private readonly ICompanyService companyService = Mock.Of<ICompanyService>();
    private readonly MockHttpMessageHandler http = new();
    private readonly DataSyncOptions dataSyncOptions;
    private readonly ICryptography cryptography;
    private readonly RoleOptions roleOptions;

    public TermsControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();
        var cryptographyOptions = new CryptographyOptions
        {
            Key = "secretsecretsecretsecret"
        };

        dataSyncOptions = configuration.GetSection(DataSyncOptions.Prefix).Get<DataSyncOptions>()!;
        roleOptions = configuration.GetSection(RoleOptions.Prefix).Get<RoleOptions>()!;
        cryptography = new Cryptography(cryptographyOptions);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldOnlyUpdateAcceptedTermsVersion_WhenUserExists()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var allowCprLookup = false;
        var oldAcceptedTermsVersion = 1;
        var newAcceptedTermsVersion = 2;
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var providerEncrypted = cryptography.Encrypt($"{providerKeyType}={providerKey}");

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(cryptography)
            {
                Id = id,
                Name = Guid.NewGuid().ToString(),
                Tin = Guid.NewGuid().ToString(),
                AllowCprLookup = true,
                EncryptedProviderKeys = providerEncrypted,
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = id,
                Name = name,
                AllowCprLookup = allowCprLookup,
                UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = oldAcceptedTermsVersion } }
            });

        http.When(HttpMethod.Post, dataSyncOptions.Uri!.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await termsController.AcceptUserTermsAsync(logger, accessor, mapper, userService, companyService, factory, dataSyncOptions, roleOptions, newAcceptedTermsVersion);
        Assert.NotNull(result);
        Assert.IsType<OkResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.UserTerms.First().AcceptedVersion == newAcceptedTermsVersion
                && y.Name == name
                && y.AllowCprLookup == allowCprLookup
                && y.Id == id)),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldCreateUser_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var companyName = Guid.NewGuid().ToString();
        var tin = Guid.NewGuid().ToString();
        var allowCprLookup = false;
        var newAcceptedTermsVersion = 1;
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var providerEncrypted = cryptography.Encrypt($"{providerKeyType}={providerKey}");

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(cryptography)
            {
                Id = id,
                Name = name,
                CompanyName = companyName,
                Tin = tin,
                AllowCprLookup = allowCprLookup,
                EncryptedProviderKeys = providerEncrypted
            });

        http.When(HttpMethod.Post, dataSyncOptions.Uri!.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await termsController.AcceptUserTermsAsync(logger, accessor, mapper, userService, companyService, factory, dataSyncOptions, roleOptions, newAcceptedTermsVersion);
        Assert.NotNull(result);
        Assert.IsType<OkResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.UserTerms.First().AcceptedVersion == newAcceptedTermsVersion
                && y.Name == name
                && y.AllowCprLookup == allowCprLookup
                && y.Id == id
                && y.Company != null
                && y.Company.Tin == tin
                && y.Company.Name == companyName
                && y.UserProviders.Count() == 1
                && y.UserProviders.First().ProviderKeyType == providerKeyType
                && y.UserProviders.First().UserProviderKey == providerKey)),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldThrowNullReferenceException_WhenUserDescriptMapperReturnsNull()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: null);

        http.When(HttpMethod.Post, dataSyncOptions.Uri!.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptUserTermsAsync(logger, accessor, mapper, userService, companyService, factory, dataSyncOptions, roleOptions, 3));
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldThrowNullReferenceException_WhenDescriptorIdExistsButUserCannotBeFound()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                Tin = null,
                AllowCprLookup = false,
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        Mock.Get(companyService)
           .Setup(x => x.GetCompanyByTinAsync(It.IsAny<string>()))
           .ReturnsAsync(value: null);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptUserTermsAsync(logger, accessor, mapper, userService, companyService, factory, dataSyncOptions, roleOptions, 2));
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldThrowArgumentException_WhenUserHasAlreadyAcceptedNewerTermsVersion()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid()
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                AllowCprLookup = false,
                UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 2 } }
            });

        await Assert.ThrowsAsync<ArgumentException>(async () => await termsController.AcceptUserTermsAsync(logger, accessor, mapper, userService, companyService, factory, dataSyncOptions, roleOptions, 1));
    }
}
