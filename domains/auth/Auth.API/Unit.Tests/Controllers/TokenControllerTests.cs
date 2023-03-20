using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Services;
using API.Utilities;
using API.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public class TokenControllerTests
{
    private readonly TokenController tokenController = new();
    private readonly ITokenIssuer issuer = Mock.Of<ITokenIssuer>();
    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly ICryptography cryptography = Mock.Of<ICryptography>();
    private readonly ClaimsPrincipal claimsPrincipal = Mock.Of<ClaimsPrincipal>();

    public TokenControllerTests() =>
        tokenController.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
        };

    [Theory]
    [InlineData(false, UserScopeClaim.NotAcceptedTerms, "625fa04a-4b17-4727-8066-82cf5b5a8b0d")]
    [InlineData(true, UserScopeClaim.AllAcceptedScopes, "625fa04a-4b17-4727-8066-82cf5b5a8b0d")]
    [InlineData(false, UserScopeClaim.NotAcceptedTerms, null)]
    public async Task RefreshAsync_ShouldIssueTokenAndReturnOkWithToken_WhenInvokedSuccessfully(bool bypass, string scope, string? userId)
    {
        var token = Guid.NewGuid().ToString();

        Mock.Get(cryptography)
            .Setup(x => x.Decrypt<string>(It.IsAny<string>()))
            .Returns(value: Guid.NewGuid().ToString());

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: new UserDescriptor(cryptography)
            {
                Id = userId != null ? Guid.Parse(userId) : null,
                EncryptedAccessToken = Guid.NewGuid().ToString(),
                EncryptedIdentityToken = Guid.NewGuid().ToString()
            });

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(value: new UserDescriptor(cryptography)
            {
                Id = Guid.NewGuid()
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: new User()
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                ProviderId = Guid.NewGuid().ToString(),
                AllowCPRLookup = false,
                AcceptedTermsVersion = 1
            });

        Mock.Get(issuer)
            .Setup(x => x.Issue(It.IsAny<UserDescriptor>(), It.IsAny<bool>(), null))
            .Returns(value: token);

        Mock.Get(claimsPrincipal)
            .Setup(x => x.FindFirst(UserClaimName.Scope))
            .Returns(value: new Claim(UserClaimName.Scope, scope));

        var result = await tokenController.RefreshAsync(mapper, userService, issuer);
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

        await Assert.ThrowsAsync<NullReferenceException>(async () => await tokenController.RefreshAsync(mapper, userService, issuer));
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowNullReferenceException_WhenDescriptorIdExistsButUserCannotBeFound()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                ProviderId = Guid.NewGuid().ToString(),
                Tin = null,
                AllowCPRLookup = false,
                AcceptedTermsVersion = 1
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await tokenController.RefreshAsync(mapper, userService, issuer));
    }
}
