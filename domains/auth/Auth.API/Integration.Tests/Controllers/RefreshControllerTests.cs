//using System.IdentityModel.Tokens.Jwt;
//using System.Net;
//using System.Security.Claims;
//using API.Options;
//using API.Utilities;
//using Microsoft.AspNetCore.TestHost;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Tests.Integration;

//namespace Integration.Tests.Controllers;

//public class RefreshControllerTests : IClassFixture<AuthWebApplicationFactory>
//{
//    private readonly AuthWebApplicationFactory factory;
//    public RefreshControllerTests(AuthWebApplicationFactory factory)
//    {
//        this.factory = factory;
//    }

//    [Fact]
//    public async Task RefreshAccessToken_ShouldReturnCookieHeaderAndNoContent_WhenInvoked()
//    {
//        var user = await factory.AddUserToDatabaseAsync();
//        var tokenIssuedAt = DateTime.UtcNow.AddMinutes(-5);
//        var client = factory.CreateAuthenticatedClient(user, issueAt: tokenIssuedAt);
//        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;
//        var tokenOptions = factory.ServiceProvider.GetRequiredService<IOptions<TokenOptions>>();

//        var result = await client.GetAsync("auth/refresh");
//        Assert.NotNull(result);
//        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

//        var header = result.Headers.GetValues("Set-Cookie");
//        Assert.True(header.Count() >= 1);
//        Assert.Contains("Authentication=", header.First());
//        Assert.Contains("; secure", header.First());
//        Assert.Contains("; expires=", header.First());

//        var newToken = header.First().Split("Authentication=").Last().Split(";").First();
//        Assert.NotEqual(oldToken, newToken);

//        var expiresDate = DateTime.Parse(header.First()!.Split("expires=").Last().Split(';').First());
//        Assert.True(DateTime.Now.AddMinutes(tokenOptions.Value.CookieDuration.TotalMinutes - 1) < expiresDate);
//        Assert.True(DateTime.Now.AddMinutes(tokenOptions.Value.CookieDuration.TotalMinutes + 1) > expiresDate);

//        var oldTokenJwt = new JwtSecurityTokenHandler().ReadJwtToken(oldToken);
//        var newTokenJwt = new JwtSecurityTokenHandler().ReadJwtToken(newToken);
//        Assert.True(oldTokenJwt.ValidFrom < newTokenJwt.ValidFrom);
//        Assert.True(oldTokenJwt.ValidTo < newTokenJwt.ValidTo);
//    }

//    [Fact]
//    public async Task RefreshAccessToken_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
//    {
//        var user = await factory.AddUserToDatabaseAsync();

//        var client = factory.CreateAuthenticatedClient(user, config: builder =>
//        {
//            var mapper = Mock.Of<IUserDescriptMapper>();
//            _ = Mock.Get(mapper)
//                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
//                .Returns(value: null!);

//            builder.ConfigureTestServices(services => services.AddScoped(x => mapper));
//        });

//        var result = await client.GetAsync("auth/refresh");

//        Assert.NotNull(result);
//        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
//    }
//}
