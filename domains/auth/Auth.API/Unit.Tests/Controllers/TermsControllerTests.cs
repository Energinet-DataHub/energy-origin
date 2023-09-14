using System.Net;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
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
    private readonly TermsController controller = new();
    private readonly ILogger<TermsController> logger = Mock.Of<ILogger<TermsController>>();
    private readonly IHttpContextAccessor accessor = Mock.Of<IHttpContextAccessor>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly IHttpClientFactory factory = Mock.Of<IHttpClientFactory>();
    private readonly ICompanyService companyService = Mock.Of<ICompanyService>();
    private readonly MockHttpMessageHandler http = new();
    private readonly DataSyncOptions dataSyncOptions;
    private readonly ICryptography cryptography;
    private readonly RoleOptions roleOptions;
    private readonly TermsOptions termsOptions;
    private readonly OidcOptions oidcOptions;

    public TermsControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        dataSyncOptions = configuration.GetSection(DataSyncOptions.Prefix).Get<DataSyncOptions>()!;
        roleOptions = configuration.GetSection(RoleOptions.Prefix).Get<RoleOptions>()!;
        termsOptions = configuration.GetSection(TermsOptions.Prefix).Get<TermsOptions>()!;
        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
        cryptography = new Cryptography(configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!);
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

        controller.PrepareUser(id: id, name: name, allowCprLookup: $"{allowCprLookup}", encryptedProviderKeys: cryptography.Encrypt($"{providerKeyType}={providerKey}"));

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = id,
                Name = name,
                AllowCprLookup = allowCprLookup,
                UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = oldAcceptedTermsVersion } }
            });

        var result = await controller.AcceptUserTermsAsync(logger, accessor, userService, companyService, factory, cryptography, dataSyncOptions, roleOptions, termsOptions, oidcOptions, newAcceptedTermsVersion);
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
        var organization = new OrganizationDescriptor
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Tin = Guid.NewGuid().ToString()
        };
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var allowCprLookup = false;
        var newAcceptedTermsVersion = 1;
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;

        controller.PrepareUser(id: id, name: name, organization: organization, allowCprLookup: $"{allowCprLookup}", encryptedProviderKeys: cryptography.Encrypt($"{providerKeyType}={providerKey}"));

        var result = await controller.AcceptUserTermsAsync(logger, accessor, userService, companyService, factory, cryptography, dataSyncOptions, roleOptions, termsOptions, oidcOptions, newAcceptedTermsVersion);
        Assert.NotNull(result);
        Assert.IsType<OkResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.UserTerms.First().AcceptedVersion == newAcceptedTermsVersion
                && y.Name == name
                && y.AllowCprLookup == allowCprLookup
                && y.Id == id
                && y.Company != null
                && y.Company.Tin == organization.Tin
                && y.Company.Name == organization.Name
                && y.UserProviders.Count() == 1
                && y.UserProviders.First().ProviderKeyType == providerKeyType
                && y.UserProviders.First().UserProviderKey == providerKey)),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldThrowNullReferenceException_WhenPrincipalIsNull() => await Assert.ThrowsAsync<PropertyMissingException>(async () => await controller.AcceptUserTermsAsync(logger, accessor, userService, companyService, factory, cryptography, dataSyncOptions, roleOptions, termsOptions, oidcOptions, 3));

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldThrowArgumentException_WhenUserHasAlreadyAcceptedNewerTermsVersion()
    {
        controller.PrepareUser();

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                AllowCprLookup = false,
                UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 2 } }
            });

        var result = await controller.AcceptUserTermsAsync(logger, accessor, userService, companyService, factory, cryptography, dataSyncOptions, roleOptions, termsOptions, oidcOptions, 1);

        Assert.NotNull(result);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
