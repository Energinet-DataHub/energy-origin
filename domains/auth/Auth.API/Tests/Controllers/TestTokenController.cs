using System;
using System.Collections.Generic;
using API.Configuration;
using API.Models;
using API.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Categories;

namespace API.Controllers;

[UnitTest]
public class TestTokenController
{
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();

    private readonly TokenController tokenController;

    public TestTokenController()
    {
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        tokenController = new TokenController(
            authOptionsMock.Object,
            tokenStorage.Object
        )
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public void ForwardAuth_OpaqueTokenAndInternalTokenExists_ReturnAuthorizationHeaderAndStatusCode200()
    {
        tokenStorage.Setup(x => x.GetInteralTokenByOpaqueToken(It.IsAny<string>())).Returns(new InternalToken
        {
            Actor = "Actor",
            Subject = "Subject",
            Scope = new List<string> { "Scope1", "Scope2" },
            Issued = DateTime.UtcNow.AddHours(-1),
            Expires = DateTime.UtcNow.AddHours(6)
        });

        var opaqueToken = "TestOpaqueToken";

        tokenController.HttpContext.Request.Headers.Add("Cookie", $"Authorization={opaqueToken}");

        var response = tokenController.ForwardAuth();

        Assert.Equal(200, ((StatusCodeResult)response).StatusCode);
    }

    [Fact]
    public void ForwardAuth_MissingOpaqueTokenInHeader_ReturnStatusCode401()
    {
        var response = tokenController.ForwardAuth();

        Assert.Equal(401, ((StatusCodeResult)response).StatusCode);
    }

    [Fact]
    public void ForwardAuth_OpaqueTokenExistsInHeaderNoInternalToken_ReturnStatusCode401()
    {
        var tokenStorage = new Mock<ITokenStorage>();
        tokenStorage.Setup(x => x.GetInteralTokenByOpaqueToken(It.IsAny<string>())).Returns((InternalToken?)null);

        var opaqueToken = "TestOpaqueToken";

        tokenController.HttpContext.Request.Headers.Add("Cookie", $"Authorization={opaqueToken}");

        var response = tokenController.ForwardAuth();

        Assert.Equal(401, ((StatusCodeResult)response).StatusCode);
    }
}
