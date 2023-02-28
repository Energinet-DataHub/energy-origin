using System.Security.Claims;
using API.Controllers;
using API.Options;
using API.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Integration.Tests.Controllers;

public class RefreshControllerTests
{
    private readonly RefreshController refreshController = new();
    private readonly IOptions<TokenOptions> tokenOptions;
    private readonly ITokenIssuer issuer = Mock.Of<ITokenIssuer>();
    private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();
    private readonly IHttpContextAccessor accessor = Mock.Of<IHttpContextAccessor>();

    public RefreshControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        tokenOptions = Options.Create(configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!);

        Mock.Get(accessor).Setup(it => it.HttpContext).Returns(new DefaultHttpContext());
    }

    [Fact]
    public void RefreshAccessToken_ShouldReturnCookieHeaderAndNoContent_WhenInvoked()
    {
        var oldToken = Guid.NewGuid().ToString();

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: new UserDescriptor(null!));

        Mock.Get(issuer)
            .Setup(x => x.Issue(It.IsAny<UserDescriptor>(), null))
            .Returns(value: oldToken);

        var result = refreshController.RefreshAccessToken(accessor, tokenOptions, mapper, issuer);
        Assert.NotNull(result);
        Assert.IsType<NoContentResult>(result);

        Assert.NotNull(accessor.HttpContext);

        var header = accessor.HttpContext!.Response.Headers.SetCookie;
        Assert.True(header.Count >= 1);
        Assert.Contains("Authentication=", header[0]);
        Assert.Contains("; secure", header[0]);
        Assert.Contains("; expires=", header[0]);

        var newToken = header.First()!.Split("Authentication=").Last().Split(";").First();
        Assert.Equal(oldToken, newToken);

        var expiresDate = DateTime.Parse(header.First()!.Split("expires=").Last().Split(';').First());
        Assert.True(DateTime.Now.AddMinutes(tokenOptions.Value.CookieDuration.TotalMinutes - 1) < expiresDate);
        Assert.True(DateTime.Now.AddMinutes(tokenOptions.Value.CookieDuration.TotalMinutes + 1) > expiresDate);
    }

    [Fact]
    public void RefreshAccessToken_ShouldThrowNullReferenceException_WhenUserDescriptMapperReturnsNull()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: null);

        Assert.Throws<NullReferenceException>(() => refreshController.RefreshAccessToken(accessor, tokenOptions, mapper, issuer));
    }
}
