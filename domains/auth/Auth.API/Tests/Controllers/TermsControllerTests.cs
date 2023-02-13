using System.Security.Claims;
using API.Controllers;
using API.Models;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers
{
    public class TermsControllerTests
    {
        private readonly TermsController termsController = new TermsController();
        private readonly IUserService userService = Mock.Of<IUserService>();
        private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();

        [Fact]
        public async Task AcceptTermsAsync_ShouldOnlyUpdateAcceptedTermsVersionAndReturnUser_WhenUserExists()
        {
            var id = Guid.NewGuid();
            var name = Guid.NewGuid().ToString();
            var providerId = Guid.NewGuid().ToString();
            var tin = null as string;
            var allowCprLookup = false;
            var oldAcceptedTermsVersion = 1;
            var newAcceptedTermsVersion = 2;

            var hehe = Guid.NewGuid().ToString();

            Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: new UserDescriptor(null!)
                {
                    Id = id,
                    Name = Guid.NewGuid().ToString(),
                    ProviderId = Guid.NewGuid().ToString(),
                    Tin = Guid.NewGuid().ToString(),
                    AllowCPRLookup = true,
                    AcceptedTermsVersion = oldAcceptedTermsVersion
                });

            Mock.Get(userService)
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: new User()
                {
                    Id = id,
                    Name = name,
                    ProviderId = providerId,
                    Tin = tin,
                    AllowCPRLookup = allowCprLookup,
                    AcceptedTermsVersion = oldAcceptedTermsVersion
                });

            Mock.Get(userService)
                .Setup(x => x.UpsertUserAsync(It.IsAny<User>()))
                .ReturnsAsync(value: new User()
                {
                    Id = id,
                    Name = name,
                    ProviderId = providerId,
                    Tin = tin,
                    AllowCPRLookup = allowCprLookup,
                    AcceptedTermsVersion = newAcceptedTermsVersion
                });

            var result = await termsController.AcceptTermsAsync(newAcceptedTermsVersion, mapper, userService);
            Assert.NotNull(result);
            Assert.IsType<ActionResult<User>>(result);
            Assert.IsType<OkObjectResult>(result?.Result);

            var value = (result?.Result as OkObjectResult)!.Value;
            Assert.NotNull(value);
            Assert.IsType<User>(value);

            var user = (value as User)!;
            Assert.Equal(id, user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(providerId, user.ProviderId);
            Assert.Equal(tin, user.Tin);
            Assert.Equal(allowCprLookup, user.AllowCPRLookup);
            Assert.Equal(newAcceptedTermsVersion, user.AcceptedTermsVersion);
        }

        [Fact]
        public async Task AcceptTermsAsync_ShouldCreateAndReturnUser_WhenUserDoesNotExist()
        {
            var id = null as Guid?;
            var name = Guid.NewGuid().ToString();
            var providerId = Guid.NewGuid().ToString();
            var tin = null as string;
            var allowCprLookup = false;
            var newAcceptedTermsVersion = 1;

            Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: new UserDescriptor(null!)
                {
                    Id = id,
                    Name = name,
                    ProviderId = providerId,
                    Tin = tin,
                    AllowCPRLookup = allowCprLookup,
                    AcceptedTermsVersion = 0
                });

            Mock.Get(userService)
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: null);

            Mock.Get(userService)
                .Setup(x => x.UpsertUserAsync(It.IsAny<User>()))
                .ReturnsAsync(value: new User()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ProviderId = providerId,
                    Tin = tin,
                    AllowCPRLookup = allowCprLookup,
                    AcceptedTermsVersion = newAcceptedTermsVersion
                });

            var result = await termsController.AcceptTermsAsync(newAcceptedTermsVersion, mapper, userService);
            Assert.NotNull(result);
            Assert.IsType<ActionResult<User>>(result);
            Assert.IsType<OkObjectResult>(result?.Result);

            var value = (result?.Result as OkObjectResult)!.Value;
            Assert.NotNull(value);
            Assert.IsType<User>(value);

            var user = (value as User)!;
            Assert.NotEqual(id, user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(providerId, user.ProviderId);
            Assert.Equal(tin, user.Tin);
            Assert.Equal(allowCprLookup, user.AllowCPRLookup);
            Assert.Equal(newAcceptedTermsVersion, user.AcceptedTermsVersion);
        }
    }
}
