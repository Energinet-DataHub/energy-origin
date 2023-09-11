using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static API.Utilities.TokenIssuer;

namespace Unit.Tests.Controllers;

public class TokenControllerTests
{
    private readonly TokenController controller = new();
    private readonly IMetrics metrics = Mock.Of<IMetrics>();
    private readonly ILogger<TokenController> logger = Mock.Of<ILogger<TokenController>>();
    private readonly ITokenIssuer issuer = Mock.Of<ITokenIssuer>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly ICryptography cryptography = Mock.Of<ICryptography>();
    private readonly ClaimsPrincipal claimsPrincipal = Mock.Of<ClaimsPrincipal>();

    public TokenControllerTests() => controller.ControllerContext = new()
    {
        HttpContext = new DefaultHttpContext { User = claimsPrincipal }
    };

    [Theory]
    [InlineData(false, UserScopeName.NotAcceptedPrivacyPolicy, "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.NemIdPrivate)]
    [InlineData(true, $"{UserScopeName.Dashboard} {UserScopeName.Production} {UserScopeName.Meters} {UserScopeName.Certificates}", "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.MitIdPrivate)]
    [InlineData(false, UserScopeName.NotAcceptedPrivacyPolicy, "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.NemIdProfessional)]
    public async Task RefreshAsync_ShouldIssueTokenAndReturnOkWithToken_WhenInvokedSuccessfully(bool bypass, string scope, string userId, ProviderType providerType)
    {
        var token = Guid.NewGuid().ToString();

        Mock.Get(cryptography)
            .Setup(x => x.Decrypt<string>(It.IsAny<string>()))
            .Returns(Guid.NewGuid().ToString());

        controller.PrepareUser(id: Guid.Parse(userId), providerType: providerType);
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(cryptography)
        //     {
        //         Id = Guid.Parse(userId),
        //         EncryptedAccessToken = Guid.NewGuid().ToString(),
        //         EncryptedIdentityToken = Guid.NewGuid().ToString(),
        //         ProviderType = providerType,
        //         MatchedRoles = ""
        //     });

        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<User>(), providerType, It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>()))
        //     .Returns(new UserDescriptor(cryptography)
        //     {
        //         Id = Guid.NewGuid()
        //     });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                AllowCprLookup = false,
                UserTerms = new List<UserTerms> { new() { AcceptedVersion = 1, Type = UserTermsType.PrivacyPolicy } }
            });

        Mock.Get(issuer)
            .Setup(x => x.Issue(It.IsAny<UserDescriptor>(), It.IsAny<UserData>(), It.IsAny<bool>(), It.IsAny<DateTime>()))
            .Returns(token);

        Mock.Get(claimsPrincipal)
            .Setup(x => x.FindFirst(UserClaimName.Scope))
            .Returns(new Claim(UserClaimName.Scope, scope));

        var result = await controller.RefreshAsync(metrics, logger, userService, issuer);
        Assert.NotNull(result);
        Assert.IsType<OkObjectResult>(result);

        Assert.Equal((result as OkObjectResult)!.Value, token);

        Mock.Get(issuer).Verify(x => x.Issue(It.IsAny<UserDescriptor>(), It.IsAny<UserData>(), bypass, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldCallMetricsTokenRefresh_WhenInvokedSuccessfully()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var providerType = ProviderType.MitIdProfessional;

        Mock.Get(cryptography)
            .Setup(x => x.Decrypt<string>(It.IsAny<string>()))
            .Returns(Guid.NewGuid().ToString());

        controller.PrepareUser();
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(cryptography)
        //     {
        //         Id = userId,
        //         CompanyId = companyId,
        //         ProviderType = providerType
        //     });

        _ = await controller.RefreshAsync(metrics, logger, userService, issuer);

        Mock.Get(metrics).Verify(x => x.TokenRefresh(
            userId,
            companyId,
            providerType),
            Times.Once
        );
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowNullReferenceException_WhenUserReturnsNull() => await Assert.ThrowsAsync<PropertyMissingException>(async () => await controller.RefreshAsync(metrics, logger, userService, issuer));
}
