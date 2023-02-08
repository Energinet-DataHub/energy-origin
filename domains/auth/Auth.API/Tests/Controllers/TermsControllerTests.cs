using System.Reflection.Metadata;
using System.Web;
using API.Controllers;
using API.Models;
using API.Options;
using API.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tests.Controllers
{
    public class TermsControllerTests
    {
        [Fact]
        public async Task AcceptTermsAsync_ShouldUpdateAcceptedTermsVersionAndReturnUser_WhenUserExists()
        {
            var userService = Mock.Of<IUserService>();

            var termsVersion = 5;

            Mock.Get(userService)
                .Setup(it => it.UpsertUserAsync(It.IsAny<User>()))
                .ReturnsAsync(value: new User()
                {
                    Id = Guid.NewGuid(),
                    ProviderId = Guid.NewGuid().ToString(),
                    Name = "Amigo",
                    AcceptedTermsVersion = termsVersion,
                    Tin = null,
                    AllowCPRLookup = true
                });

            var result = await new TermsController().AcceptTermsAsync(termsVersion, userService);
            var user = result.Value;

            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            Assert.Equal(termsVersion, user?.AcceptedTermsVersion);




            //var options = TestOptions.Oidc(oidcOptions);

            //var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.Value.AuthorityUri.Host}/connect") });

            //var cache = Mock.Of<IDiscoveryCache>();
            //_ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

            //var logger = Mock.Of<ILogger<LoginController>>();

            //var result = await new LoginController().LoginAsync(cache, options, logger);

            //Assert.NotNull(result);
            //Assert.IsType<RedirectResult>(result);

            //var redirectResult = (RedirectResult)result;
            //Assert.True(redirectResult.PreserveMethod);
            //Assert.False(redirectResult.Permanent);

            //var uri = new Uri(redirectResult.Url);
            //Assert.Equal(options.Value.AuthorityUri.Host, uri.Host);

            //var query = HttpUtility.UrlDecode(uri.Query);
            //Assert.Contains($"client_id={options.Value.ClientId}", query);
            //Assert.Contains($"redirect_uri={options.Value.AuthorityCallbackUri.AbsoluteUri}", query);
        }
    }
}
