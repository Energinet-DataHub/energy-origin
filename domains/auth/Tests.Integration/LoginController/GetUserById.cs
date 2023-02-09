using System.Net;

namespace Tests.Integration.LoginController
{
    public class GetUserById : IClassFixture<LoginApiFactory>
    {
        private readonly LoginApiFactory factory;
        public GetUserById(LoginApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task GetList_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var client = factory.CreateUnauthenticatedClient();
            var userResponse = await client.GetAsync("G");
            var df = await client.GetAsync("GetUserByProviderId/1");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
        }

        [Fact]
        public async Task Test()
        {
           await Task.Delay(5000);
        }
    }
}
