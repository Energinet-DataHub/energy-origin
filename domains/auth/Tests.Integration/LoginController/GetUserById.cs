using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc.Testing;

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
        public async Task G()
        {
            var client = factory.CreateUnauthenticatedClient();
            var userResponse = await client.GetAsync("G");

            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
        }

        [Fact]
        public async Task W()
        {
            var client = factory.CreateUnauthenticatedClient();
            var userResponse = await client.GetAsync("W");

            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
        }

        [Fact]
        public async Task Test()
        {
           await Task.Delay(5000);
        }
    }
}
