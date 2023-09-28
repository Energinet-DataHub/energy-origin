using API.Controllers;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static API.Utilities.TokenIssuer;

namespace Unit.Tests.Controllers;

public class TokenControllerTests
{
    private readonly TokenController controller = new();
    private readonly IMetrics metrics = Substitute.For<IMetrics>();
    private readonly ILogger<TokenController> logger = Substitute.For<ILogger<TokenController>>();
    private readonly ITokenIssuer issuer = Substitute.For<ITokenIssuer>();
    private readonly IUserService userService = Substitute.For<IUserService>();
    private readonly ICryptography cryptography = Substitute.For<ICryptography>();

    [Theory]
    [InlineData(false, UserScopeName.NotAcceptedPrivacyPolicy, "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.NemIdPrivate)]
    [InlineData(true, $"{UserScopeName.Dashboard} {UserScopeName.Production} {UserScopeName.Meters} {UserScopeName.Certificates}", "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.MitIdPrivate)]
    [InlineData(false, UserScopeName.NotAcceptedPrivacyPolicy, "625fa04a-4b17-4727-8066-82cf5b5a8b0d", ProviderType.NemIdProfessional)]
    public async Task RefreshAsync_ShouldIssueTokenAndReturnOkWithToken_WhenInvokedSuccessfully(bool bypass, string scope, string userId, ProviderType providerType)
    {
        var token = Guid.NewGuid().ToString();

        cryptography.Decrypt<string>(Arg.Any<string>()).Returns(Guid.NewGuid().ToString());

        controller.PrepareUser(id: Guid.Parse(userId), providerType: providerType, scope: scope);

        userService.GetUserByIdAsync(Arg.Any<Guid>()).Returns(new User
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            CompanyId = Guid.NewGuid(),
            UserTerms = new List<UserTerms> { new() { AcceptedVersion = 1, Type = UserTermsType.PrivacyPolicy } }
        });

        issuer.Issue(Arg.Any<UserDescriptor>(), Arg.Any<UserData>(), Arg.Any<bool>(), Arg.Any<DateTime>()).Returns(token);

        var result = await controller.RefreshAsync(metrics, logger, userService, issuer);
        Assert.NotNull(result);
        Assert.IsType<OkObjectResult>(result);

        Assert.Equal((result as OkObjectResult)!.Value, token);

        issuer.Received(1).Issue(Arg.Any<UserDescriptor>(), Arg.Any<UserData>(), bypass, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task RefreshAsync_ShouldCallMetricsTokenRefresh_WhenInvokedSuccessfully()
    {
        var organization = new OrganizationDescriptor
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Tin = Guid.NewGuid().ToString()
        };
        var userId = Guid.NewGuid();
        var providerType = ProviderType.MitIdProfessional;

        controller.PrepareUser(id: userId, organization: organization, providerType: providerType);

        _ = await controller.RefreshAsync(metrics, logger, userService, issuer);

        metrics.Received(1).TokenRefresh(
            userId,
            organization.Id,
            providerType
        );
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowNullReferenceException_WhenPrincipalIsNull() => await Assert.ThrowsAsync<PropertyMissingException>(async () => await controller.RefreshAsync(metrics, logger, userService, issuer));
}
