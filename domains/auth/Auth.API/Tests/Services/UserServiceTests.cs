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
        private readonly UserService sut;
        private readonly IUserRepository repositoryMock = Mock.Of<IUserRepository>();
        private readonly ILogger<UserService> loggerMock = Mock.Of<ILogger<UserService>>();

        public UserServiceTests()
        {
            sut = new UserService(repositoryMock, loggerMock);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenUserExists()
        {
            var id = Guid.NewGuid();

            Mock.Get(repositoryMock)
                .Setup(it => it.GetUserById(It.IsAny<Guid>()))
                .ReturnsAsync(value: new User()
                {
                    Id = id,
                    ProviderId = "1",
                    Name = "Amigo",
                    AcceptedTermsVersion = "Version 4",
                    Tin = null,
                    AllowCPRLookup = true
                });

            var result = await sut.GetUserById(id);

            Assert.Equal(id, result?.Id);
        }

        [Fact]
        public async Task GetUserByProviderId_ShouldReturnUser_WhenUserExists()
        {
            var providerId = "1";

            Mock.Get(repositoryMock)
                .Setup(it => it.GetUserByProviderId(It.IsAny<string>()))
                .ReturnsAsync(value: new User()
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    Name = "Amigo",
                    AcceptedTermsVersion = "Version 4",
                    Tin = null,
                    AllowCPRLookup = true
                });

            var result = await sut.GetUserByProviderId(providerId);

            Assert.Equal(providerId, result?.ProviderId);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnNull_WhenNoUserExists()
        {
            Mock.Get(repositoryMock)
                .Setup(it => it.GetUserById(It.IsAny<Guid>()))
                .ReturnsAsync(value: null);

            var result = await sut.GetUserById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByProviderId_ShouldReturnNull_WhenNoUserExists()
        {
            Mock.Get(repositoryMock)
                .Setup(it => it.GetUserByProviderId(It.IsAny<string>()))
                .ReturnsAsync(value: null);

            var result = await sut.GetUserByProviderId("1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserById_ShouldLogErrorAndThrowException_WhenExceptionIsThrown()
        {
            Mock.Get(repositoryMock)
                .Setup(it => it.GetUserById(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception());

            await Assert.ThrowsAsync<Exception>(async () => await sut.GetUserById(Guid.NewGuid()));

            Mock.Get(loggerMock).Verify(it => it.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserByProviderId_ShouldLogErrorAndThrowException_WhenExceptionIsThrown()
        {
            Mock.Get(repositoryMock)
                .Setup(it => it.GetUserByProviderId(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            await Assert.ThrowsAsync<Exception>(async () => await sut.GetUserByProviderId("1"));

            Mock.Get(loggerMock).Verify(it => it.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }
    }
}
