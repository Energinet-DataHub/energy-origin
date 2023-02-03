using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Controllers;
using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Web;
using API.Services;
using API.Repositories;
using System.Reflection.Metadata;
using API.Models;
using Serilog.Core;

namespace Tests.Services
{
    public class UserServiceTests
    {
        private readonly IUserService userService;
        private readonly IUserRepository repository = Mock.Of<IUserRepository>();
        private readonly ILogger<IUserService> logger = Mock.Of<ILogger<IUserService>>();

        public UserServiceTests()
        {
            userService = new UserService(repository, logger);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenUserExists()
        {
            var id = Guid.NewGuid();

            Mock.Get(repository)
                .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: new User()
                {
                    Id = id,
                    ProviderId = Guid.NewGuid().ToString(),
                    Name = "Amigo",
                    AcceptedTermsVersion = 2,
                    Tin = null,
                    AllowCPRLookup = true
                });

            var result = await userService.GetUserByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result?.Id);
        }

        [Fact]
        public async Task GetUserByProviderId_ShouldReturnUser_WhenUserExists()
        {
            var providerId = Guid.NewGuid().ToString();

            Mock.Get(repository)
                .Setup(it => it.GetUserByProviderIdAsync(It.IsAny<string>()))
                .ReturnsAsync(value: new User()
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    Name = "Amigo",
                    AcceptedTermsVersion = 2,
                    Tin = null,
                    AllowCPRLookup = true
                });

            var result = await userService.GetUserByProviderIdAsync(providerId);

            Assert.NotNull(result);
            Assert.Equal(providerId, result?.ProviderId);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnNull_WhenNoUserExists()
        {
            Mock.Get(repository)
                .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: null);

            var result = await userService.GetUserByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByProviderId_ShouldReturnNull_WhenNoUserExists()
        {
            Mock.Get(repository)
                .Setup(it => it.GetUserByProviderIdAsync(It.IsAny<string>()))
                .ReturnsAsync(value: null);

            var result = await userService.GetUserByProviderIdAsync(Guid.NewGuid().ToString());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserById_ShouldLogErrorAndThrowException_WhenExceptionIsThrown()
        {
            Mock.Get(repository)
                .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception());

            await Assert.ThrowsAsync<Exception>(async () => await userService.GetUserByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetUserByProviderId_ShouldLogErrorAndThrowException_WhenExceptionIsThrown()
        {
            Mock.Get(repository)
                .Setup(it => it.GetUserByProviderIdAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            await Assert.ThrowsAsync<Exception>(async () => await userService.GetUserByProviderIdAsync(Guid.NewGuid().ToString()));
        }
    }
}
