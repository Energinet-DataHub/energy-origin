using System.Security.Claims;
using API.Controllers;
using API.Models.DTOs;
using API.Models.Entities;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public class TermsControllerTests
{
    private readonly TermsController termsController = new();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();

    [Fact]
    public async Task AcceptTermsAsync_ShouldOnlyUpdateAcceptedTermsVersion_WhenUserExists()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid().ToString();
        var tin = null as string;
        var allowCprLookup = false;
        var oldAcceptedTermsVersion = 1;
        var newAcceptedTermsVersion = 2;

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

        var result = await termsController.AcceptTermsAsync(mapper, userService, new AcceptTermsDTO(newAcceptedTermsVersion));
        Assert.NotNull(result);
        Assert.IsType<NoContentResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.AcceptedTermsVersion == newAcceptedTermsVersion
                && y.Name == name
                && y.ProviderId == providerId
                && y.Tin == tin
                && y.AllowCPRLookup == allowCprLookup
                && y.Id == id)),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldCreateUser_WhenUserDoesNotExist()
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

        var result = await termsController.AcceptTermsAsync(mapper, userService, new AcceptTermsDTO(newAcceptedTermsVersion));
        Assert.NotNull(result);
        Assert.IsType<NoContentResult>(result);

        Mock.Get(userService).Verify(x => x.UpsertUserAsync(
            It.Is<User>(y =>
                y.AcceptedTermsVersion == newAcceptedTermsVersion
                && y.Name == name
                && y.ProviderId == providerId
                && y.Tin == tin
                && y.AllowCPRLookup == allowCprLookup
                && y.Id == id)),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldThrowNullReferenceException_WhenUserDescriptMapperReturnsNull()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: null);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptTermsAsync(mapper, userService, new AcceptTermsDTO(1)));
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldThrowNullReferenceException_WhenDescriptorIdExistsButUserCannotBeFound()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                ProviderId = Guid.NewGuid().ToString(),
                Tin = null,
                AllowCPRLookup = false,
                AcceptedTermsVersion = 1
            });

        Mock.Get(userService)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptTermsAsync(mapper, userService, new AcceptTermsDTO(1)));
    }
}
