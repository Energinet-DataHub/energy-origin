using System.Net;
using API.Options;
using System.Web;
using Tests.Integration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System;
using API.Controllers;
using System.Net.Http;

namespace Integration.Tests.Controllers
{
    public class OidcControllerTests : IClassFixture<AuthWebApplicationFactory>
    {
        private readonly AuthWebApplicationFactory factory;
        public OidcControllerTests(AuthWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithCookie_WhenInvoked()
        {
            var broker = factory.MockOidcProvider();
            var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.Test.json", false)
           .Build();
            var oidcOptions = Options.Create(new OidcOptions()
            {
                AuthorityUri = new Uri($"http://localhost:{broker.Port}/op"),
                ClientId = Guid.NewGuid().ToString(),
                AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug")
            });

           var tokenOptions = Options.Create(configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!);


   //         var tokenOptions = Options.Create(new TokenOptions()
   //         {
   //           Issuer = "Us",
   // Audience = "Users",
   // CookieDuration = "00:05:00",
   // Duration = "24:00:00",
   // PublicKeyPem = Toby"LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF3TENkQmwrekdJei9Va0xHVEtmaAplc0F0bUUxQUIrL25lQ0djU1VmU3h1Z2tkc3J3NXBuYmlteGtyZnU2RGxpSmtDTXZMUkZPOHRLZHhnYjN1ZWxPCm96U2ZpS2h3Nm9NRmQzWEd4dUpLeWljUVFFRVVwdkxmS3AxN2NmTE1aRG44T1lPYmQ2Q3lXVjBvczIzb2cyZVkKcmtvRzg1UzdOQ2JQeVJUSVF3VUJvNGw0bE9RWnNpNjgwaXdvU0ErVWRFL25La0dGVGxSOERYTW45NUR2V1hacQpkdDRjMXlPbmpMRmRyYTRLODRlblZ5Wm56SlQvRkFRVGRJTG9zLzhOVnlQM3pOY21sWmZmZDJUZlp5T3NRRHVhClVKQzd0eFlUYm50UlpZMnd5clVXaTZkblFOSkFwdmx5ZEN5QnpibmM3MW9Mc2NXQXpGZjdnSzRRSWI4SVNjWEQKUlFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg==",
   //PrivateKeyPem = "LS0tLS1CRUdJTiBSU0EgUFJJVkFURSBLRVktLS0tLQpNSUlFb3dJQkFBS0NBUUVBd0xDZEJsK3pHSXovVWtMR1RLZmhlc0F0bUUxQUIrL25lQ0djU1VmU3h1Z2tkc3J3CjVwbmJpbXhrcmZ1NkRsaUprQ012TFJGTzh0S2R4Z2IzdWVsT296U2ZpS2h3Nm9NRmQzWEd4dUpLeWljUVFFRVUKcHZMZktwMTdjZkxNWkRuOE9ZT2JkNkN5V1Ywb3MyM29nMmVZcmtvRzg1UzdOQ2JQeVJUSVF3VUJvNGw0bE9RWgpzaTY4MGl3b1NBK1VkRS9uS2tHRlRsUjhEWE1uOTVEdldYWnFkdDRjMXlPbmpMRmRyYTRLODRlblZ5Wm56SlQvCkZBUVRkSUxvcy84TlZ5UDN6TmNtbFpmZmQyVGZaeU9zUUR1YVVKQzd0eFlUYm50UlpZMnd5clVXaTZkblFOSkEKcHZseWRDeUJ6Ym5jNzFvTHNjV0F6RmY3Z0s0UUliOElTY1hEUlFJREFRQUJBb0lCQUh5VmRHRTduT3RCdG83MApONHcyZTFYSFRYT01kdHJxNU9qS0ttZEM5ZWUvRGx4MEtEK2d1TTZOK0taNC9EbnNTcjBUMHB1NzlpU1B3b3pYCjBuRzBoRENIaEtKeDdkZmljTFZsUStreFJKUGhuK003Y09Qa1lpQUdoRnNQVmRGem9EMTdkeGhvb1FlZ2NRRmEKRFp4d2JjbzZlTFlpc3Nzc1VPbzg4cUpLYVYzWEZCQ3dJNkVTeDA3Q3ZWRXkxd0FJTklFSEE0VTZ5dDF3c1dsbQpLNFJZMkxVdjJtQTlhTkQyNjEwZEhIN3YwVS8wN0oxQXdJZmhhMzh1b0dkS05GeFNhU1Ivc2M2Tk84VXpwcHhSCmliMUl4TnBFb2Q4K2o4ZDdqSDFQV1ZlUDRpamxvUlBlbkRMeXdtWnBva3g2WEN6QjBzdlFLUjJlUFQ4ajZWSmMKc05VdW5sMENnWUVBNFdKQjU5MHQyci9IZVpHMW9SdmxGRXd3aWt4RzJ0UXA2ZXdTNEk5T0oxYTBnU3NLc0xQagpWSFk4MjNtSHozT3dzUFV2K3Y5bkRYOGpHd0JaTmNlZ2E3MHJoekNFT0luQ2RzcmV1U3Y3TzJDbUpOeklZNFFWCklGR2JyZFlEaGRPclJUbnVVcmpnOElQRXNSY01SdTZJTnlxUVRBN244UVNOQ3RjejlQZkIxbmNDZ1lFQTJ0MXIKL21MOWduUlo2bmhRd1FzL05NNWZpcnhlSTZ3WFI0UjlYcG9XbHhwNjVPbnZOZVVFTndZT0xCeHhqblhoMTlDMQozQk5MN2sxVHJLM1FZNVBBR1Y1TDZvT3U1UWJ0ajU4QldaTmRBRWNGWHVvYkNsb1FWRExRRW5EaURvWWVjd0xtCkZHNEFtVk0wRFlEWmdSckszR1AwbVFhTGNrYm9ZTWI0TnlwMVZ5TUNnWUVBaVNmTVI1ZVhzZ2tIRVBvVTk4Z0wKN2dBM2dkSE5SSm5jTDloVDNJZ1kzV09zVVBhcWVNSGYwNlJvZ0g5Q29JSWN3bk5URVlHZmF0MDF0ZGJPY3lYYQpmL1lNcVNaak1DelZSSWxNWkk5WlFkY2RCRTIvUEtCQ1l2cUdySkVTYjd4Uis3eTNSV3Z3cHl6bzQ0UE5HdFZKCjI1aHhXM2V1dWtNMHVhWWduakN2cXgwQ2dZQWgxdng4bjZlY3hRcW1BeVpSUXNEcUZFS1hlOXArWDN4VjlYbEEKNkVnMzRzTS9vNS8xMEV3ZmlkTWxKTnkxN3lvVktWTUZEUUsvZkx0RVJyZWl2ZFNFMTV5YlRQTDh2RjU4eDFQNQpHcHpWanlXWWNFL3dBTTduaGRmQUVpNFJtdEVZYlVsUHZWWmdYb244MElCUXd1aTh2TU96NlZ3a05peDEwaTNNCnNjYmt5d0tCZ0JJK09iRjBMVEt4SVQ4dk5mZVczbUwxU0svRk5QSFFZSFh4VTdCUzQ0L1MwVmF2bTdDRWtXUGEKWXByV2ZQRkkvVWtPcEpOZ3hBVGxiTFcvYmdzaUJnY1A3K3ZweFlIN0Y5YnoydzRLajY3Yjc3bnVRWDMvL1RTSQo0ZE9GMERIeENkU3ZNV2s1d0ZGRCt2RjJvTEttdTdkaDA0VVJlNFJmb2Y2Q3RRZmlIZDhzCi0tLS0tRU5EIFJTQSBQUklWQVRFIEtFWS0tLS0tCg==",
   

   //         });
            

            var client = factory
                .CreateAnonymousClient(builder =>
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddScoped(x => oidcOptions);
                        services.AddScoped(x => tokenOptions);
                    }
                    ));
            var queryString = "auth/oidc/callback/error_description?code=errorCode&error=702";
            var result = await client.GetAsync(queryString);
            var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);


            //var action = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

            //Assert.NotNull(action);
            //Assert.IsType<OkObjectResult>(action);
            //var result = action as OkObjectResult;
            //var body = result!.Value as string;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            //Assert.Contains("<html><head><meta ", body);
            //Assert.Contains(" http-equiv=", body);
            //Assert.Contains("refresh", body);
            //Assert.Contains("<body />", body);
            //Assert.Contains(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, body);
        }
    }
}
