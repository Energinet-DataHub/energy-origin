using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Controllers;

[UsesVerify]
public class InvitationsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>, IClassFixture<WalletContainer>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;
    private readonly string sub;

    public InvitationsControllerTests(TransferAgreementsApiWebApplicationFactory factory, WalletContainer wallet)
    {
        this.factory = factory;
        sub = Guid.NewGuid().ToString();
        factory.WalletUrl = wallet.WalletUrl;
        authenticatedClient = factory.CreateAuthenticatedClient(sub);
    }

    [Fact]
    public async Task Create_ShouldReturnInvitationId_WhenInvitationIsCreated()
    {

        var result = await authenticatedClient
            .PostAsync("api/invitations", null);

        var response = await result.Content.ReadAsStringAsync();

        var settings = new VerifySettings();
        settings.ScrubInlineGuids();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        await Verifier.Verify(response, settings);
    }

    [Fact]
    public async Task CreateInvitation_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.PostAsync("api/invitations", null);

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
