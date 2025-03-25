using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Services;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class AddOrganizationToWhitelistCommandHandlerTests
{
    [Fact]
    public async Task Given_ValidTin_When_HandleIsCalled_Then_CallsAuthorizationService()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var handler = new AddOrganizationToWhitelistCommandHandler(mockAuthorizationService);
        var testTin = "12345678";
        var command = new AddOrganizationToWhitelistCommand { Tin = testTin };

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        await mockAuthorizationService.Received(1)
            .AddOrganizationToWhitelistHttpRequestAsync(testTin);
        Assert.True(result);
    }

    [Fact]
    public async Task Given_ServiceFails_When_HandleIsCalled_Then_ReturnsFalse()
    {
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        var handler = new AddOrganizationToWhitelistCommandHandler(mockAuthorizationService);
        var testTin = "12345678";
        var command = new AddOrganizationToWhitelistCommand { Tin = testTin };

        mockAuthorizationService
            .When(x => x.AddOrganizationToWhitelistHttpRequestAsync(testTin))
            .Do(_ => throw new HttpRequestException("Simulated API failure"));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result);
    }
}
