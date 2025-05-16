using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Services;
using EnergyOrigin.Domain.ValueObjects;
using NSubstitute;

namespace AdminPortal.Tests.Features;

public class AddOrganizationToWhitelistCommandHandlerTests
{
    [Fact]
    public async Task Given_ValidTin_When_HandleIsCalled_Then_CallsAuthorizationService()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var handler = new AddOrganizationToWhitelistCommandHandler(mockAuthorizationService);
        var testTin = Tin.Create("12345678");
        var command = new AddOrganizationToWhitelistCommand(testTin);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        await mockAuthorizationService.Received(1)
            .AddOrganizationToWhitelistAsync(testTin, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ServiceFails_When_HandleIsCalled_Then_ExceptionIsPropagated()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var handler = new AddOrganizationToWhitelistCommandHandler(mockAuthorizationService);
        var testTin = Tin.Create("12345678");
        var command = new AddOrganizationToWhitelistCommand(testTin);

        mockAuthorizationService
            .When(x => x.AddOrganizationToWhitelistAsync(testTin, Arg.Any<CancellationToken>()))
            .Do(_ => throw new HttpRequestException("Simulated API failure"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
