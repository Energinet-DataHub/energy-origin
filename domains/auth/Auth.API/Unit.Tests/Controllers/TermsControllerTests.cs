using System.Net;
using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Models.Requests;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Unit.Tests.Controllers;

public class TermsControllerTests
{
    private readonly TermsController termsController = new();
    private readonly ILogger<TermsController> logger = Mock.Of<ILogger<TermsController>>();
    private readonly IHttpContextAccessor accessor = Mock.Of<IHttpContextAccessor>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly IHttpClientFactory factory = Mock.Of<IHttpClientFactory>();
    private readonly ICompanyService companyService = Mock.Of<ICompanyService>();
    private readonly MockHttpMessageHandler http = new();
    private readonly IOptions<DataSyncOptions> options;
    private readonly ICryptography cryptography;

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

        options = Moptions.Create(configuration.GetSection(DataSyncOptions.Prefix).Get<DataSyncOptions>()!);
        cryptography = new Cryptography(Moptions.Create(cryptographyOptions));
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
        var providerKeyType = ProviderKeyType.MitID_UUID;
        var providerEncrypted = cryptography.Encrypt($"{providerKeyType}={providerKey}");

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(cryptography)
            {
                Id = id,
                Name = Guid.NewGuid().ToString(),
                Tin = Guid.NewGuid().ToString(),
                AllowCPRLookup = true,
                AcceptedTermsVersion = oldAcceptedTermsVersion,
                EncryptedProviderKeys = providerEncrypted,
                UserStored = true
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = id,
                Name = name,
                AllowCprLookup = allowCprLookup,
                AcceptedTermsVersion = oldAcceptedTermsVersion
            });

        http.When(HttpMethod.Post, options.Value.Uri!.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, companyService, factory, options, new AcceptTermsRequest(newAcceptedTermsVersion));
        Assert.NotNull(result);
        Assert.IsType<NoContentResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.AcceptedTermsVersion == newAcceptedTermsVersion
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
        var providerKeyType = ProviderKeyType.MitID_UUID;
        var providerEncrypted = cryptography.Encrypt($"{providerKeyType}={providerKey}");

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(cryptography)
            {
                Id = id,
                Name = name,
                CompanyName = companyName,
                Tin = tin,
                AllowCPRLookup = allowCprLookup,
                AcceptedTermsVersion = 0,
                EncryptedProviderKeys = providerEncrypted,
                UserStored = false
            });

        http.When(HttpMethod.Post, options.Value.Uri!.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, companyService, factory, options, new AcceptTermsRequest(newAcceptedTermsVersion));
        Assert.NotNull(result);
        Assert.IsType<NoContentResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.AcceptedTermsVersion == newAcceptedTermsVersion
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
            .Returns(null);

        http.When(HttpMethod.Post, options.Value.Uri!.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, companyService, factory, options, new AcceptTermsRequest(1)));
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
                AllowCPRLookup = false,
                AcceptedTermsVersion = 1
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(null);

        Mock.Get(companyService)
           .Setup(x => x.GetCompanyByTinAsync(It.IsAny<string>()))
           .ReturnsAsync(null);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, companyService, factory, options, new AcceptTermsRequest(2)));
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldThrowArgumentException_WhenUserHasAlreadyAcceptedNewerTermsVersion()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                AcceptedTermsVersion = 2
            });

        await Assert.ThrowsAsync<ArgumentException>(async () => await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, companyService, factory, options, new AcceptTermsRequest(1)));
    }
}
