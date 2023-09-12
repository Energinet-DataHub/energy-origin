using System;
using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Controllers;

[UsesVerify]
public class ConnectionInvitationsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly string sub;

    public ConnectionInvitationsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid().ToString();
        factory.WalletUrl = "UnusedWalletUrl";
    }

    [Fact]
    public async Task Create_ShouldReturnInvitationId_WhenInvitationIsCreated()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var result = await authenticatedClient
            .PostAsync("api/connection-invitations", null);

        var response = await result.Content.ReadAsStringAsync();

        var settings = new VerifySettings();
        settings.ScrubInlineGuids();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        await Verifier.Verify(response, settings);
    }

    [Fact]
    public async Task CreateConnectionInvitation_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.PostAsync("api/connection-invitations", null);

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
