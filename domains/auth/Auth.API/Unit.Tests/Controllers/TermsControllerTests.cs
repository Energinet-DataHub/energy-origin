using System.Net;
using System.Security.Claims;
using API.Controllers;
using API.Models.DTOs;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Tests.Controllers;

public class TermsControllerTests
{
    private readonly TermsController termsController = new();
    private readonly ILogger<TermsController> logger = Mock.Of<ILogger<TermsController>>();
    private readonly IHttpContextAccessor accessor = Mock.Of<IHttpContextAccessor>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();
    private readonly IHttpClientFactory factory = Mock.Of<IHttpClientFactory>();
    private readonly MockHttpMessageHandler http = new();
    private readonly IOptions<DataSyncOptions> options;

    public TermsControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        options = Options.Create(configuration.GetSection(DataSyncOptions.Prefix).Get<DataSyncOptions>()!);
    }

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

        http.When(HttpMethod.Post, options.Value.Uri.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, factory, options, new AcceptTermsDTO(newAcceptedTermsVersion));
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

        http.When(HttpMethod.Post, options.Value.Uri.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, factory, options, new AcceptTermsDTO(newAcceptedTermsVersion));
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

        http.When(HttpMethod.Post, options.Value.Uri.AbsoluteUri).Respond(HttpStatusCode.OK);
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, factory, options, new AcceptTermsDTO(1)));
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

        await Assert.ThrowsAsync<NullReferenceException>(async () => await termsController.AcceptTermsAsync(logger, accessor, mapper, userService, factory, options, new AcceptTermsDTO(1)));
    }
}
