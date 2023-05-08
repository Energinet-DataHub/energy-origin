using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class TokenControllerTests
{
    private readonly TokenController tokenController = new();
    private readonly ILogger<TokenController> logger = Mock.Of<ILogger<TokenController>>();
    private readonly ITokenIssuer issuer = Mock.Of<ITokenIssuer>();
    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly ICryptography cryptography = Mock.Of<ICryptography>();
    private readonly ClaimsPrincipal claimsPrincipal = Mock.Of<ClaimsPrincipal>();

    public TokenControllerTests() =>
        tokenController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

    [Theory]
    [InlineData(false, UserScopeClaim.NotAcceptedTerms, "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.NemID_Private, true)]
    [InlineData(true, $"{UserScopeClaim.AcceptedTerms} {UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.MitID_Private, true)]
    [InlineData(false, UserScopeClaim.NotAcceptedTerms, "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.NemID_Professional, false)]
    public async Task RefreshAsync_ShouldIssueTokenAndReturnOkWithToken_WhenInvokedSuccessfully(bool bypass, string scope, string userId, ProviderType providerType, bool isStored)
    {
        var token = Guid.NewGuid().ToString();

        Mock.Get(cryptography)
            .Setup(x => x.Decrypt<string>(It.IsAny<string>()))
            .Returns(Guid.NewGuid().ToString());

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(cryptography)
            {
                Id = Guid.Parse(userId),
                EncryptedAccessToken = Guid.NewGuid().ToString(),
                EncryptedIdentityToken = Guid.NewGuid().ToString(),
                UserStored = isStored
            });

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<User>(), providerType, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new UserDescriptor(cryptography)
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
                AcceptedTermsVersion = 1
            });

        Mock.Get(issuer)
            .Setup(x => x.Issue(It.IsAny<UserDescriptor>(), It.IsAny<bool>(), null))
            .Returns(token);

        Mock.Get(claimsPrincipal)
            .Setup(x => x.FindFirst(UserClaimName.Scope))
            .Returns(new Claim(UserClaimName.Scope, scope));

        var result = await tokenController.RefreshAsync(logger, mapper, userService, issuer);
        Assert.NotNull(result);
        Assert.IsType<OkObjectResult>(result);

        Assert.Equal((result as OkObjectResult)!.Value, token);

        Mock.Get(issuer).Verify(x => x.Issue(It.IsAny<UserDescriptor>(), bypass, null), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowNullReferenceException_WhenUserDescriptMapperReturnsNull()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: null);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await tokenController.RefreshAsync(logger, mapper, userService, issuer));
    }
}
